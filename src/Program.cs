using Discord;
using Discord.WebSocket;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;
using Path = System.IO.Path;
using System.Xml.Linq;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient? _client;
        private readonly List<string> _notifiedVideos = []; // Lista de vídeos já notificados

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
            #if DEBUG
            DotNetEnv.Env.Load(".env.dev");
            Console.WriteLine("Arquivo .env.dev carregado (modo DEBUG).");
            #endif

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | 
                                 GatewayIntents.GuildMembers | 
                                 GatewayIntents.GuildMessages | 
                                 GatewayIntents.MessageContent
            });

            _client.Log += LogAsync;
            _client.UserJoined += UserJoinedAsync;

            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Erro: Token do Discord não encontrado. Defina a variável de ambiente 'DISCORD_TOKEN'.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Inicia o monitoramento de uploads no YouTube
            // Verifica se as variáveis para a funcionalidade do YouTube estão configuradas
            string? youtubeChannelId = Environment.GetEnvironmentVariable("YOUTUBE_CHANNEL_ID");
            string? discordChannelIdStr = Environment.GetEnvironmentVariable("YOUTUBE_DISCORD_CHANNEL_ID");

            if (!string.IsNullOrEmpty(youtubeChannelId) && !string.IsNullOrEmpty(discordChannelIdStr))
            {
                Console.WriteLine("Configurações do YouTube encontradas. Monitoramento de uploads ativado.");
                await MonitorYouTubeUploadsAsync(youtubeChannelId, discordChannelIdStr);
            }
            else
            {
                Console.WriteLine("Configurações do YouTube não encontradas. Funcionalidade de monitoramento de uploads será desativada.");
            }

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        public async Task MonitorYouTubeUploadsAsync(string youtubeChannelId, string discordChannelIdStr)
        {
            string messageTemplate = Environment.GetEnvironmentVariable("YOUTUBE_MESSAGE_TEMPLATE") ??
                                    "Novo vídeo no canal {author}: {title}\n{url}";

            if (!ulong.TryParse(discordChannelIdStr, out ulong discordChannelId))
            {
                Console.WriteLine("Erro: ID do canal do Discord é inválido. Funcionalidade de monitoramento desativada.");
                return;
            }

            var httpClient = new HttpClient();
            string feedUrl = $"https://www.youtube.com/feeds/videos.xml?channel_id={youtubeChannelId}";

            // Inicializar o serviço Firebase
            var firebaseService = new FirebaseService();

            while (true)
            {
                try
                {
                    // Obter o feed RSS do YouTube
                    var response = await httpClient.GetStringAsync(feedUrl);
                    var xDoc = XDocument.Parse(response);

                    var ns = XNamespace.Get("http://www.w3.org/2005/Atom");
                    var latestVideo = xDoc.Descendants(ns + "entry").FirstOrDefault();

                    if (latestVideo != null)
                    {
                        string videoUrl = latestVideo.Element(ns + "link")?.Attribute("href")?.Value ?? string.Empty;
                        string videoTitle = latestVideo.Element(ns + "title")?.Value ?? "Título desconhecido";
                        string videoAuthor = latestVideo.Element(ns + "author")?.Element(ns + "name")?.Value ?? "Autor desconhecido";

                        // Verificar se o vídeo já foi notificado
                        if (!await firebaseService.IsVideoNotifiedAsync(videoUrl))
                        {
                            // Adicionar à lista de notificados no Firebase
                            await firebaseService.AddNotifiedVideoAsync(videoUrl);

                            // Enviar mensagem ao Discord (caso configurado)
                            if (_client?.GetChannel(discordChannelId) is IMessageChannel discordChannel)
                            {
                                string message = messageTemplate
                                    .Replace("{author}", videoAuthor)
                                    .Replace("{title}", videoTitle)
                                    .Replace("{url}", videoUrl);

                                await discordChannel.SendMessageAsync(message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao monitorar uploads: {ex.Message}");
                }

                await Task.Delay(30000); // Intervalo de 30 segundos
            }
        }



       
        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            Console.WriteLine("Novo membro entrou no servidor.");

            string welcomeChannelIdStr = Environment.GetEnvironmentVariable("WELCOME_CHANNEL_ID") ?? string.Empty;

            if (!string.IsNullOrEmpty(welcomeChannelIdStr) && ulong.TryParse(welcomeChannelIdStr, out ulong welcomeChannelId))
            {
                Console.WriteLine($"ID do canal de boas-vindas: {welcomeChannelId}");
            }
            else
            {
                Console.WriteLine("Erro: 'WELCOME_CHANNEL_ID' não é um número válido ou não está definido.");
                return;
            }            

            var welcomeChannel = user.Guild.GetTextChannel(welcomeChannelId);
            if (welcomeChannel != null)
            {
                var avatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl();
                var cardPath = await GenerateWelcomeCardAsync(user.Username, user.Guild.MemberCount, avatarUrl);

                if (!string.IsNullOrEmpty(cardPath))
                {
                    await welcomeChannel.SendFileAsync(cardPath, $"Olá {user.Mention}, seja bem-vindo(a) ao nosso servidor!");
                    File.Delete(cardPath);
                }
            }
            else
            {
                Console.WriteLine("Canal de boas-vindas não encontrado.");
            }
        }

        private async Task<string?> GenerateWelcomeCardAsync(string username, int memberNumber, string avatarUrl)
        {
            int width = 600;
            int height = 400;
            string filePath = Path.Combine(Path.GetTempPath(), $"welcome_card_{Guid.NewGuid()}.png");

            // Obter URL da imagem de fundo, texto de boas-vindas, cor de fundo e cor da fonte
            string backgroundUrl = Environment.GetEnvironmentVariable("BACKGROUND_IMAGE_URL") ?? string.Empty;
            string welcomeText = Environment.GetEnvironmentVariable("WELCOME_TEXT") ?? "Bem-vindo(a) ao Servidor!";
            string backgroundColorHex = Environment.GetEnvironmentVariable("BACKGROUND_COLOR") ?? "#000000"; // Preto
            string fontColorHex = Environment.GetEnvironmentVariable("FONT_COLOR") ?? "#FFFFFF"; // Branco

            try
            {
                using (var image = new Image<Rgba32>(width, height))
                {
                    if (!string.IsNullOrEmpty(backgroundUrl))
                    {
                        using var httpClient = new HttpClient();
                        using var backgroundStream = await httpClient.GetStreamAsync(backgroundUrl);
                        using var backgroundImage = Image.Load<Rgba32>(backgroundStream);
                        var resizeOptions = new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Mode = ResizeMode.Crop
                        };

                        backgroundImage.Mutate(ctx => ctx.Resize(resizeOptions).Opacity(0.15f));
                        image.Mutate(ctx => ctx.DrawImage(backgroundImage, new Point(0, 0), 1f));
                    }
                    else
                    {
                        var backgroundColor = Color.ParseHex(backgroundColorHex);
                        image.Mutate(ctx => ctx.Fill(backgroundColor));
                    }

                    // Baixar e aplicar a imagem do avatar
                    using (var httpClient = new HttpClient())
                    using (var avatarStream = await httpClient.GetStreamAsync(avatarUrl))
                    using (var avatarImage = Image.Load<Rgba32>(avatarStream))
                    {
                        int avatarSize = 180;
                        avatarImage.Mutate(ctx => ctx.Resize(avatarSize, avatarSize).ApplyRoundedCorners(avatarSize / 2));

                        int avatarX = (width - avatarSize) / 2;
                        int avatarY = 40;
                        image.Mutate(ctx => ctx.DrawImage(avatarImage, new Point(avatarX, avatarY), 1f));
                    }

                    var fontCollection = new FontCollection();
                    var fontFamily = fontCollection.Add("assets/fonts/DejaVuSans.ttf");
                    var font = fontFamily.CreateFont(24, FontStyle.Bold);

                    // Converter a cor da fonte
                    var fontColor = Color.ParseHex(fontColorHex);

                    // Configuração de centralização do nome do usuário
                    var usernameOptions = new RichTextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(width / 2, 250)
                    };
                    image.Mutate(ctx => ctx.DrawText(usernameOptions, username, fontColor));

                    var memberOptions = new RichTextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(width / 2, 290)
                    };
                    string memberText = $"Membro número: {memberNumber}";
                    image.Mutate(ctx => ctx.DrawText(memberOptions, memberText, fontColor));

                    var welcomeOptions = new RichTextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(width / 2, 340)
                    };
                    image.Mutate(ctx => ctx.DrawText(welcomeOptions, welcomeText, fontColor));

                    image.Save(filePath, new SixLabors.ImageSharp.Formats.Png.PngEncoder());
                }

                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerar o card de boas-vindas: {ex.Message}");
                return null;
            }
        }
    }
}
