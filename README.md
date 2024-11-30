
# Discord App Welcome Message

[![.NET](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/dotnet.yml)  
[![CodeQL Advanced](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/codeql.yml/badge.svg)](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/codeql.yml)  
[![Dependabot Updates](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/dependabot/dependabot-updates/badge.svg)](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/dependabot/dependabot-updates)

Aplicação criada em ASP.NET Core para enviar mensagens de boas-vindas personalizadas aos novos membros de um servidor Discord e monitorar uploads de vídeos de um canal do YouTube, enviando notificações automaticamente.

---

## Funcionalidades

1. **Mensagem de boas-vindas**:
   - Personalizável com imagem de fundo, cor sólida e texto configurável.
2. **Monitoramento de uploads do YouTube**:
   - Periodicamente verifica um canal do YouTube e notifica um canal do Discord sobre novos vídeos.

---

## Requisitos

- **Discord Bot Token**: Obtenha no [Discord Developer Portal](https://discord.com/developers/applications).
- **ID do Canal de Boas-Vindas**: ID do canal no Discord onde as mensagens de boas-vindas serão enviadas.
- **YouTube Channel ID**: ID do canal do YouTube que será monitorado.
- **ID do Canal de Notificações do Discord**: ID do canal no Discord para receber notificações de uploads.

---

## Configuração

### Variáveis de Ambiente

| Variável                | Descrição                                                                                  | Obrigatória |
|-------------------------|--------------------------------------------------------------------------------------------|-------------|
| `DISCORD_TOKEN`         | Token do bot do Discord.                                                                  | Sim         |
| `WELCOME_CHANNEL_ID`    | ID do canal no Discord para mensagens de boas-vindas.                                      | Sim         |
| `WELCOME_TEXT`          | Texto da mensagem de boas-vindas.                                                          | Não         |
| `BACKGROUND_IMAGE_URL`  | URL da imagem de fundo para o cartão de boas-vindas.                                       | Não         |
| `BACKGROUND_COLOR`      | Cor sólida de fundo (hexadecimal, ex.: `#000000`).                                         | Não         |
| `FONT_COLOR`            | Cor do texto no cartão de boas-vindas (hexadecimal, ex.: `#FFFFFF`).                       | Não         |
| `YOUTUBE_CHANNEL_ID`    | ID do canal do YouTube que será monitorado.                                                | Não         |
| `YOUTUBE_DISCORD_CHANNEL_ID`    | ID do canal do Discord para notificações de uploads.                                       | Não         |
| `YOUTUBE_MESSAGE_TEMPLATE`      | Modelo de mensagem para notificações de vídeos. Ex.: `{author}: {title}\n{url}`.           | Não         |
| `YOUTUBE_WATCH_INTERVAL`        | Intervalo em milissegundos para verificar novos vídeos no YouTube (padrão: `30000`).       | Não         |

> **Nota:** As funcionalidades do YouTube são opcionais e desativadas automaticamente se as variáveis `YOUTUBE_CHANNEL_ID` ou `DISCORD_CHANNEL_ID` não forem configuradas.

---

## Rodando Localmente

1. Clone o repositório:
   ```bash
   git clone https://github.com/CafeCOMaquina/discord-app-welcome-message.git
   cd discord-app-welcome-message
   ```

2. Configure as variáveis de ambiente no arquivo `.env`:
   ```env
   DISCORD_TOKEN=seu-token-aqui
   WELCOME_CHANNEL_ID=id-do-canal-aqui
   WELCOME_TEXT=Bem-vindo(a) ao nosso servidor!
   YOUTUBE_CHANNEL_ID=id-do-canal-youtube
   YOUTUBE_DISCORD_CHANNEL_ID=id-do-canal-discord
   ```

3. Execute o projeto:
   ```bash
   dotnet run
   ```

---

## Usando Docker

### Construir a Imagem Docker

```bash
docker build -t discord-app-welcome-message .
```

### Executar com Docker

```bash
docker run -d \
  -e DISCORD_TOKEN=<seu-token> \
  -e WELCOME_CHANNEL_ID=<id-do-canal> \
  -e YOUTUBE_CHANNEL_ID=<id-do-canal-youtube> \
  -e YOUTUBE_DISCORD_CHANNEL_ID=<id-do-canal-discord> \
  -e YOUTUBE_MESSAGE_TEMPLATE="Novo vídeo no canal {author}: {title}\n{url}" \
  cafecomaquina/discord-app-welcome-message:latest
```

### Usando Docker Compose

Exemplo de configuração:

```yaml
version: '3.8'

services:
  discord-bot:
    image: cafecomaquina/discord-app-welcome-message:latest
    environment:
      DISCORD_TOKEN: "<seu-discord-token>"
      WELCOME_CHANNEL_ID: "<id-do-canal>"
      WELCOME_TEXT: "Bem-vindo(a) ao nosso servidor!"
      YOUTUBE_CHANNEL_ID: "<id-do-canal-youtube>"
      YOUTUBE_DISCORD_CHANNEL_ID: "<id-do-canal-discord>"
      YOUTUBE_MESSAGE_TEMPLATE: "Novo vídeo no canal {author}: {title}\n{url}"
    restart: always
```

Inicie com:
```bash
docker-compose up -d
```

---

## Notas

- Certifique-se de que o bot tem permissões suficientes para acessar os canais configurados.
- Teste o bot em um ambiente de desenvolvimento antes de colocá-lo em produção.
- O monitoramento do YouTube depende de feeds RSS e do intervalo configurado (`WATCH_INTERVAL`).

---

## Contribuições

Contribuições são bem-vindas! Abra issues ou envie pull requests com melhorias ou correções.

---

## Licença

Este projeto é licenciado sob a [Licença MIT](LICENSE).

---
