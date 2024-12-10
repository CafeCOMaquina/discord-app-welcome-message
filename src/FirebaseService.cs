using Firebase.Database;
using Google.Apis.Auth.OAuth2;
using FirebaseAdmin;
using Firebase.Database.Query;

public class FirebaseService
{
    private readonly FirebaseClient _firebaseClient;

    public FirebaseService()
    {
        // Carregar a URL do Firebase e as credenciais do ambiente
        string firebaseUrl = Environment.GetEnvironmentVariable("FIREBASE_URL");
        string firebaseCredentials = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");

        if (string.IsNullOrEmpty(firebaseUrl))
        {
            throw new Exception("A variável de ambiente 'FIREBASE_URL' não foi configurada.");
        }

        if (string.IsNullOrEmpty(firebaseCredentials))
        {
            throw new Exception("As credenciais do Firebase não foram encontradas na variável de ambiente 'FIREBASE_CREDENTIALS'.");
        }

        // Inicializar o FirebaseApp, se ainda não estiver inicializado
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(firebaseCredentials)
            });
        }

        // Configurar o FirebaseClient
        _firebaseClient = new FirebaseClient(
            firebaseUrl,
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = async () =>
                {
                    var credential = GoogleCredential.FromJson(firebaseCredentials)
                                                    .CreateScoped("https://www.googleapis.com/auth/firebase.database");
                    return await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                }
            });

    }

    // Adicionar um vídeo à lista de notificados
    public async Task AddNotifiedVideoAsync(string videoUrl)
    {
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            throw new ArgumentException("O URL do vídeo não pode estar vazio.");
        }

        // Salvar o URL do vídeo como um objeto JSON
        await _firebaseClient
            .Child("notifiedVideos")
            .PostAsync(new { Url = videoUrl });
    }



    // Recuperar todos os vídeos já notificados
    public async Task<List<string>> GetNotifiedVideosAsync()
    {
        var notifiedVideos = await _firebaseClient
            .Child("notifiedVideos")
            .OnceAsync<string>();

        return notifiedVideos.Select(v => v.Object).ToList();
    }

    // Verificar se um vídeo já foi notificado
    public async Task<bool> IsVideoNotifiedAsync(string videoUrl)
    {
        // Obter todos os vídeos já notificados
        var notifiedVideos = await _firebaseClient
            .Child("notifiedVideos")
            .OnceAsync<dynamic>();

        // Percorrer os vídeos e verificar se o URL já está presente
        foreach (var video in notifiedVideos)
        {
            if (video.Object.Url == videoUrl)
            {
                return true;
            }
        }

        return false;
    }

}
