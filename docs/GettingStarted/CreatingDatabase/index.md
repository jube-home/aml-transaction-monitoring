---
layout: default
title: Creating Database
nav_order: 2
parent: Getting Started
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Creating Postgres Database

It is possible to install PostgreSQL to a Droplet on a self managed basis,  which is a cost effective and oftentimes preferable approach.  The resources are available at the following location:

[https://www.postgresql.org/download/](https://www.postgresql.org/download/)

PostgreSQL is a remarkable database and is not especially difficult to maintain, although it has tendency to become more anxious when it comes to assuring backups and high availability.  The maintenance of high availability and backups is the main value proposition of Managed PostgreSQL on [DigitalOcean](https://m.do.co/c/8be72e86abb2) or alternative hosting provider.

For the purpose of this documentation,  PostgreSQL Managed Database at [DigitalOcean](https://m.do.co/c/8be72e86abb2) will be used.

In the [DigitalOcean](https://m.do.co/c/8be72e86abb2) Control Panel, navigate to Databases on the left hand navigation:

![Image](LocationOfDatabase.png)

Click on the Database menu item to expose the page to begin database creation:

![Image](GettingStartedWithManagedDatabase.png)

Locate the prominent Create Database Cluster button:

![Image](LocationOfCreateDatabaseClusterButton.png)

Click the button to expose the Create Database Cluster:

![Image](CreateDatabaseClusterPage.png)

Specify a datacenter nearby (in this case Frankfurt) and PostgreSQL version 14:

![Image](PostgreSQLInFrankfurt.png)

Scroll down to size the server, selecting in this case 2vCPU and 4GB RAM: 

![Image](SelectingDatabaseSize.png)

No standby is needed although it would be suggested given production use.  Scroll down and note the Create Database Cluster button:

![Image](LocationOfCreateDatabaseClusterButtonFinalisation.png)

Click the Create Database Cluster button to begin the process of database creation:

![Image](CreatedPage.png)

Upon the database having been created note the availability of the connection settings:

![Image](LocationOfDatabaseCredentials.png)

Clicking the show button will expose the password:

```text
username = doadmin
password = *******
host = db-postgresql-fra1-34965-do-user-12376516-0.b.db.ondigitalocean.com
port = 25060
database = defaultdb
sslmode = require
```

The database settings which will be passed as an Environment Variable later in the documentation.

Network security is outside the scope of this documentation; however, note that it is sensible to restrict access to the database to only the remote IP address in use, although this will not affect connections via the local network:

![Image](LocationOfSecurity.png)

# Creating Redis Database
The following is for completeness, however,  this guide does not suggest using Redis on Digital Ocean unless comfortable configuring TLS as follows:

https://www.digitalocean.com/community/tutorials/how-to-connect-to-managed-redis-over-tls-with-stunnel-and-redis-cli

In the same Databases context menu, proceed to create a new database as if it were Postgres SQL as above,  instead selecting Redis a the database product type:

![Image](SelectingRedisNotPostgres.png)

Select the desired size as specified in the introduction:

![Image](RedisSize.png)

Before scrolling down to confirm the creation of the Redis database:

![Image](ConfirmCreationOfRedis.png)

Once created,  the credentials for the Redis database exist in a very similar location:

![Image](LocationOfRedisCredentials.png)

The senstive information is exposed in the same manner as with Postgres,  clicking show. Hive off the following information for the installation of Jube as the final step of this process:

```text
username = default
password = ************************ show
host = db-redis-fra1-14927-do-user-12376516-0.c.db.ondigitalocean.com
port = 25061
```