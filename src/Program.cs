using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Linq; // Para manipulações como buscar cargos
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
            _client.MessageReceived += MessageReceivedAsync; // Adiciona manipulador para quiz
            _client.UserJoined += UserJoinedAsync;

            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Erro: Token do Discord não encontrado. Defina a variável de ambiente 'DISCORD_TOKEN'.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            await Task.Delay(-1);
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            if (message.Content.StartsWith("!startquiz"))
            {
                if (message.Channel is SocketTextChannel channel && message.Author is SocketGuildUser user)
                {
                    await StartQuizAsync(channel, user);
                }
            }
        }

        private async Task StartQuizAsync(SocketTextChannel channel, SocketGuildUser user)
        {
            var questions = new[]
            {
                new { Question = "Qual é a capital da França?", Answer = "Paris", RoleName = "Geografia Guru" },
                new { Question = "Quanto é 5+7?", Answer = "12", RoleName = "Matemático" },
                new { Question = "Quem pintou a Mona Lisa?", Answer = "Leonardo da Vinci", RoleName = "Artista Curioso" }
            };

            foreach (var q in questions)
            {
                await channel.SendMessageAsync(q.Question);

                var response = await WaitForResponse(channel, user);

                if (response.Content.Equals(q.Answer, StringComparison.OrdinalIgnoreCase))
                {
                    await channel.SendMessageAsync($"Correto! Você ganhou o cargo: {q.RoleName}");

                    var role = channel.Guild.Roles.FirstOrDefault(r => r.Name == q.RoleName);
                    if (role != null)
                    {
                        await user.AddRoleAsync(role);
                    }
                    else
                    {
                        await channel.SendMessageAsync($"O cargo `{q.RoleName}` não foi encontrado. Por favor, crie-o.");
                    }
                }
                else
                {
                    await channel.SendMessageAsync("Resposta errada! Vamos para a próxima pergunta.");
                }
            }

            await channel.SendMessageAsync("Quiz finalizado! Obrigado por participar.");
        }

        private async Task<SocketMessage> WaitForResponse(SocketTextChannel channel, SocketGuildUser user)
        {
            var tcs = new TaskCompletionSource<SocketMessage>();

            Task Func(SocketMessage message)
            {
                if (message.Author.Id == user.Id && message.Channel.Id == channel.Id)
                {
                    tcs.SetResult(message);
                }
                return Task.CompletedTask;
            }

            _client.MessageReceived += Func;
            var response = await tcs.Task;
            _client.MessageReceived -= Func;

            return response;
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
