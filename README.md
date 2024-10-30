# Discord App Welcome Message
Aplicação criada em ASP.NET Core para escutar canal do Discord e enviar mensagem de boas vindas.

Parametrizavel:
1 - Mensagem de voas vindas
2 - Plano de fundo da mensagem
3 - ID do canal para enviar mensagem
4 - token
5 - Background com foto ou COR
6 - Cor do texto


# Para rodar localmente
dotnet run


## DOCKER

# Para dar build da imagem docker
docker build -t discord-app-welcome-messsage .

# Para publicar no docker hub (somente autorizados no dockerhub)
 docker build -t discord-app-welcome-message . ;  docker tag discord-app-welcome-message cafecomaquina/discord-app-welcome-message:latest ; docker push cafecomaquina/discord-app-welcome-message:latest


# Docker Compose

version: '3.4'

services:

    discord-app-welcome-messsage:
        container_name: discord-app-welcome-messsage
        image: discord-app-welcome-messsage
        mem_limit: 2g
        restart: always
        environment:
          DISCORD_TOKEN: xxxxxxxxxxxxxxxxxxxxxxxxxxxx
          WELCOME_CHANNEL_ID: xxxxxxxxxxxxxxxxxxxxxxxxxxxx
          WELCOME_TEXT: Bem-vindo(a) ao Servidor!
          BACKGROUND_IMAGE_URL: #OPCIONAL
          BACKGROUND_COLOR: #OPCIONAL HEXADECIMAL EX: #CCCCCC 
          FONT_COLOR: #OPCIONAL HEXADECIMAL EX: #CCCCCC 
          