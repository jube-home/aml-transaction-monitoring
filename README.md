![Image](logo.png)

🚀Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.

# Introduction

Jube is open-source, real-time, Anti-Money Laundering and Fraud Detection Transaction Monitoring software. Jube empowers
organizations with enterprise-grade monitoring capabilities through a powerful combination of real-time
processing, artificial intelligence, and automated decision-making. Purpose-built for AML, fraud prevention and abuse
detection, Jube delivers comprehensive case management in an open-source package.

# Quickstart

A Docker Compose file is available—it is docker-compose.yml in the root directory—to quickly set up and orchestrate
an installation of Jube, provided Docker is already
installed. Via Docker Compose. Jube can be up and running in just a few minutes:

```shell
git clone https://github.com/jube-home/aml-fraud-transaction-monitoring
cd aml-fraud-transaction-monitoring
export DockerComposePostgresPassword='SuperSecretPasswordToChangeForPg'
export DockerComposeRabbitMQPassword='SuperSecretPasswordToChangeForAmqp'
export DockerComposeJWTKey='IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mkm1oMC50TvAPi%{mUt<9sBm>DPjGZyfYYWssseVrNUqLQE}mz{L_UsingThisKeyIsDangerous'
export DockerComposePasswordHashingKey='IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mkm1oMC50TvAPi%{mUt<9sBm>DPjGZyfYYWssseVrNUqLQE}mz{L_UsingThisKeyIsDangerous'
docker compose up -d
```

Waiting a few moments more will ensure that the embedded Kestrel web server is started correctly. In a web browser,
navigate to the bound URL [https://localhost:5001/](https://localhost:5001/) as per the ASPNETCORE_URLS Environment
Variable.

The default username \ password combination is Administrator \ Administrator, although the password will be need to be
changed on the first login.

# Documentation

A more comprehensive installation guide is available in
the [Getting Started](https://jube-home.github.io/aml-fraud-transaction-monitoring/GettingStarted) of
the [documentation](https://jube-home.github.io/aml-fraud-transaction-monitoring).

# Licence

Jube is licenced under AGPL-3.0-or-later.