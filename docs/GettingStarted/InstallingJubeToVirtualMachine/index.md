---
layout: default
title: Installing Jube To Virtual Machine
nav_order: 5
parent: Getting Started
---

🚀Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/training) from the developer.

# Installing Jube To Virtual Machine

Start by cloning the project from Github and changing to the executable directory:

```shell
git clone https://github.com/jube-home/aml-transaction-monitoring
cd jube/Jube.App
```

Noting the credentials from the Managed Postgres Database:

```text
username = doadmin
password = *******
host = db-postgresql-fra1-32549-do-user-12376516-0.b.db.ondigitalocean.com
port = 25060
database = defaultdb
sslmode = require
```

Arrange the credentials into a connection string format as follows to be passed as an Environment Variable:

```shell
export ConnectionString="Host=<host>;Port=<port>;Database=<defaultdb>;Username=<username>;Password=<password>;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;"
```

Or rather:

```shell
export ConnectionString="Host=db-postgresql-fra1-52677-do-user-12376516-0.b.db.ondigitalocean.com;Port=25060;Database=defaultdb; Username=doadmin;Password=*******;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;"
```

In the case of Redis:

```text
username = default
password = ************************ show
host = db-redis-fra1-14927-do-user-12376516-0.c.db.ondigitalocean.com
port = 25061
```

Arrange the credentials into a connection string format as follows to be passed as an Environment Variable:

```shell
export RedisConnectionString="<Host>:<Port>,password=<Password>"
```

Or rather:

```shell
export RedisConnectionString="db-redis-fra1-14927-do-user-12376516-0.c.db.ondigitalocean.com:25061,password=************************"
```

Use Redis on DigitalOcean only if comfortable with the content as follows:

https://www.digitalocean.com/community/tutorials/how-to-connect-to-managed-redis-over-tls-with-stunnel-and-redis-cli

Otherwise do not use Redis in the context of this guide,  falling back to PostgreSQL cache:

```shell
export Redis="False"
``

Execute the shell command to create the Environment Variable.

The User Interface and API endpoints need to passed the URL to bind to.  In this example, the public IP of the Droplet is 164.92.193.229 as observed in the Droplet page:

![Image](LocationOfPublicIP.png)

It is recommended that Jube always be behind a reverse proxy henceforth non standard ports are used,  in this case port 5001.  It follows that the Jube User Interface and API Endpoint will listen on https://164.92.193.229:5001/ which will be set in the ASPNETCORE_URLS Environment Variable:

```shell
export ASPNETCORE_URLS="https://143.198.224.41:5001/"
```

For security, there is no means to pass configuration values via anything other than Environment Variables,  and the contents of those Environment Variables are never - ever - stored by Jube (which is something the CodeQL security scanner tests for).

There are sensitive cryptographic values that need to be included at startup. At a minimum the JWTKey value is required:

```shell
export JWTKey="IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mk|1oMC50TvAPi%{mUt<9"B&|>DP|GZy"YYWeVrNUqLQE}mz{L_UsingThisKeyIsDangerous"
```

The JWTKey value is used to encrypt access tokens providing for API authentication, and therefore user interface authentication.

While outside of the scope of this installation documentation, other sensitive variables, while optional,  are strongly suggested:

```shell
export PasswordHashingKey="IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mk|1oMC50TvAPi%{mUt<9"B&|>DP|GZy"YYWeVrNUqLQE}mz{L_UsingThisKeyIsDangerous"
```

It is imperative that the keys be changed from their defaults and kept safe in appropriate storage.  Jube will not start if the keys above are used.

Execute the shell command to create the Environment Variable.

For the purpose of illustration the dotnet run command will be used, however, for production it is extremely unlikely that this would be satisfactory,  instead it is far more likely that a formal release will be built for a specific operating system.  There is such an abundance of release techniques for .Net applications that it exists outside the scope of this documentation.  The dotnet run command will build then run the application and it is perfectly adequate for comprehensive testing, proof of concept and smaller production implementations.  

Build and run Jube:

```shell
dotnet run
```

It will take some time to run firstly as the project will be compiled after which the startup message will be written out to the console.

Waiting a few moments will ensure that the Kestrel web server is properly started and the routing configurations are in place.  In a web browser navigate to the biding https://164.92.193.229:5001/ set in the ASPNETCORE_URLS Environment Variable.

The default user name \ password combination is Administrator \ Administrator,  although the password will be need to be changed on first login.