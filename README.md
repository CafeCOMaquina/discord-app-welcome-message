
# Discord App Welcome Message

[![.NET](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/dotnet.yml/badge.svg)](https://github.com/CafeCOMaquina/discord-app-welcome-message/actions/workflows/dotnet.yml)

Aplicação criada em ASP.NET Core para monitorar um canal do Discord e enviar uma mensagem de boas-vindas aos novos membros. A mensagem de boas-vindas é configurável e pode incluir uma imagem de fundo ou uma cor sólida, além de opções de personalização para o texto.

## Parametrização

O bot suporta as seguintes personalizações por meio de variáveis de ambiente:

1. **Mensagem de boas-vindas**
2. **Plano de fundo da mensagem** (imagem ou cor)
3. **ID do canal** onde a mensagem será enviada
4. **Token de autenticação** do bot do Discord
5. **Imagem ou cor de fundo**
6. **Cor do texto**

## Para Rodar Localmente

Para executar o projeto localmente, utilize o comando:

```bash
dotnet run
```

## DOCKER

### Para Criar a Imagem Docker

Construa a imagem Docker localmente com o seguinte comando:

```bash
docker build -t discord-app-welcome-message .
```

### Para Publicar no Docker Hub (apenas usuários autorizados)

Usuários autorizados podem publicar a imagem no Docker Hub utilizando:

```bash
docker build -t discord-app-welcome-message . 
docker tag discord-app-welcome-message cafecomaquina/discord-app-welcome-message:latest
docker push cafecomaquina/discord-app-welcome-message:latest
```

### Executando com Docker

Para iniciar o bot com Docker, utilize as variáveis de ambiente para configurar o comportamento do bot. Exemplo de execução:

```bash
docker run -d \
  -e DISCORD_TOKEN=<seu-discord-token> \
  -e WELCOME_CHANNEL_ID=<id-do-canal> \
  -e WELCOME_TEXT="Bem-vindo(a) ao nosso servidor!" \
  -e BACKGROUND_IMAGE_URL=<url-da-imagem-de-fundo> \
  -e BACKGROUND_COLOR="#000000" \
  -e FONT_COLOR="#FFFFFF" \
  --name discord-bot \
  cafecomaquina/discord-app-welcome-message:latest
```

### Exemplo com Docker Compose

Para facilitar o gerenciamento, você pode usar o Docker Compose:

```yaml
version: '3.8'

services:
  discord-bot:
    image: cafecomaquina/discord-app-welcome-message:latest
    environment:
      DISCORD_TOKEN: "<seu-discord-token>"
      WELCOME_CHANNEL_ID: "<id-do-canal>"
      WELCOME_TEXT: "Bem-vindo(a) ao nosso servidor!"
      BACKGROUND_IMAGE_URL: "<url-da-imagem-de-fundo>"
      BACKGROUND_COLOR: "#000000"
      FONT_COLOR: "#FFFFFF"
    restart: always
```

Inicie o contêiner com:

```bash
docker-compose up -d
```

## Notas

- Certifique-se de que o token do Discord e o ID do canal de boas-vindas estão corretos.
- O bot precisa de permissões de acesso ao canal onde as mensagens de boas-vindas serão enviadas.
- Teste o bot em um ambiente de desenvolvimento usando o arquivo `.env.dev` para garantir que as variáveis de ambiente estejam corretas antes de implementá-lo em produção.

## Contribuições

Contribuições são bem-vindas! Sinta-se à vontade para abrir issues e pull requests para melhorias ou correções de bugs. Por favor, siga as diretrizes de contribuição e consulte qualquer documentação relevante antes de enviar seu PR.

## Licença

Este projeto é licenciado sob a [Licença MIT](LICENSE).

---

Para mais informações sobre o desenvolvimento deste bot, consulte a documentação e os exemplos no repositório. Se tiver dúvidas, entre em contato com o mantenedor.
