#!/bin/bash

# Add the dotnet apt-get feed for the OS
release=$(lsb_release -rs)
echo "$release"

if [[ "$release" == '14.04' ]]; then
    sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ trusty main" > /etc/apt/sources.list.d/dotnetdev.list'
    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893
elif [[ "$release" == '16.04' ]]; then
    sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ xenial main" > /etc/apt/sources.list.d/dotnetdev.list'
    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893
else
    sudo sh -c 'echo "deb [arch=amd64] https://apt-mo.trafficmanager.net/repos/dotnet-release/ yakkety main" > /etc/apt/sources.list.d/dotnetdev.list'
    sudo apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 417A0893
fi

sudo apt-get update


# Install .NET Core SDK
sudo apt-get install dotnet-dev-1.0.4

# install postgres
sudo apt-get install postgresql postgresql-contrib

# configure initial postgres user/db
sudo -i -u postgres psql < scripts/pgsetup.sql

# install nginx
sudo apt-get install nginx

# configure nginx 
sudo ufw allow 'Nginx Full'
sudo cp -rf scripts/nginxconfig /etc/nginx/sites-available/default 
sudo nginx -s reload
