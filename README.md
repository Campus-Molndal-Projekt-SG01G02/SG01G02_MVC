# SG01G02_MVC

# MVC App
Follows the DDD Architecture. The app is a simple MVC app that allows the user to create, read, update and delete a list of items. The app is built using ASP.NET Core MVC and Entity Framework Core. The app uses connected to PostgreSQL Server that runs on an Azure VM and is hosted on Docker.

The app is built using the following technologies:
- ASP.NET Core MVC
- Entity Framework Core ??
- PostgreSQL in Docker
- Docker

# Ci/Cd Pipeline for MVC App
Builds the MVC App into a Docker Image and sends it to the Docker Hub. Watchtower is running on the VM Appserver that will pull the image and restart the container if it detects a new image. The app is running on port 8080. And it checks every 30 seconds for a new image. 