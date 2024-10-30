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

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient? _client;

        public static Task Main(string[] args) => new Program().MainAsync();

        public async Task MainAsync()
        {
           // Carrega o arquivo .env.dev somente em modo DEBUG
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

            await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_TOKEN") );
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task UserJoinedAsync(SocketGuildUser user)
        {
            Console.WriteLine("Novo membro entrou no servidor.");

            string welcomeChannelIdStr = Environment.GetEnvironmentVariable("WELCOME_CHANNEL_ID") ?? string.Empty;

            if (!string.IsNullOrEmpty(welcomeChannelIdStr) && ulong.TryParse(welcomeChannelIdStr, out ulong welcomeChannelId))
            {
                // Conversão bem-sucedida, você pode usar `welcomeChannelId`
                Console.WriteLine($"ID do canal de boas-vindas: {welcomeChannelId}");
            }
            else
            {
                Console.WriteLine("Erro: 'WELCOME_CHANNEL_ID' não é um número válido ou não está definido.");
                return;
            }            

            var welcomeChannel = user.Guild.GetTextChannel(welcomeChannelId); // Substitua pelo ID do canal de boas-vindas
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

            // Obter URL da imagem de fundo e o texto de boas-vindas
            string backgroundUrl = Environment.GetEnvironmentVariable("BACKGROUND_IMAGE_URL") ?? string.Empty;
            string welcomeText = Environment.GetEnvironmentVariable("WELCOME_TEXT") ?? "Bem-vindo(a) ao Servidor!";
            string localBackgroundPath = "assets/images/bg.webp"; // Caminho da imagem padrão no contêiner

            try
            {
                using (var image = new Image<Rgba32>(width, height))
                {
                    // Escolher entre a imagem de fundo local ou uma URL externa
                    if (!string.IsNullOrEmpty(backgroundUrl))
                    {
                        // Baixar a imagem de fundo da URL
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
                        // Carregar a imagem de fundo local
                        using var backgroundImage = Image.Load<Rgba32>(localBackgroundPath);
                        var resizeOptions = new ResizeOptions
                        {
                            Size = new Size(width, height),
                            Mode = ResizeMode.Crop
                        };

                        backgroundImage.Mutate(ctx => ctx.Resize(resizeOptions).Opacity(0.15f));
                        image.Mutate(ctx => ctx.DrawImage(backgroundImage, new Point(0, 0), 1f));
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

                    // Carregar e aplicar o texto
                    var fontCollection = new FontCollection();
                    var fontFamily = fontCollection.Add("assets/fonts/DejaVuSans.ttf"); // Fonte fixa
                    var font = fontFamily.CreateFont(24, FontStyle.Bold);

                    // Configuração de centralização do nome do usuário
                    var usernameOptions = new RichTextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(width / 2, 250)
                    };
                    image.Mutate(ctx => ctx.DrawText(usernameOptions, username, Color.White));

                    var memberOptions = new RichTextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(width / 2, 290)
                    };
                    string memberText = $"Membro número: {memberNumber}";
                    image.Mutate(ctx => ctx.DrawText(memberOptions, memberText, Color.LightGray));

                    var welcomeOptions = new RichTextOptions(font)
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Origin = new PointF(width / 2, 340)
                    };
                    image.Mutate(ctx => ctx.DrawText(welcomeOptions, welcomeText, Color.White));

                    // Salva o card como arquivo temporário
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











