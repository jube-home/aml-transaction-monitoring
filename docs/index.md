---
layout: default
title: Welcome
nav_order: 1
---

![Image](logo.png)

🚀Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/training) from the developer.

# Open-Source Anti-Money Laundering (AML) Transaction Monitoring

Jube is an open-source, real-time transaction and event monitoring software designed to support **Anti-Money
Laundering (AML)** compliance efforts. It aligns with
the [Jube AML Monitoring Compliance Guidance](https://jube.io/JubeAMLMonitoringComplianceGuidance.pdf), helping
organizations adapt to regulatory obligations based on **FATF guidelines** and the **Wolfsberg Principles**. While
focused on AML, Jube also addresses other transaction monitoring use cases, leveraging similar methodologies with slight
variations.

# Documentation

The [Jube documentation](https://jube-home.github.io/jube) is comprehensive and designed to cover all features, ensuring
there is no undocumented know-how. It adopts an **instructional style**, providing step-by-step explanations with
extensive use of **screenshots** to guide users through each feature.

# Quickstart with Docker Compose

A Docker Compose file is available - it is docker-compose.yml in the root directory - to quickly set up and orchestrate
an installation of Jube, provided Docker is already
installed. This Docker Compose file creates and configures the following components:

* postgres image and start.
* redis/redis-stack:latest image and start.
* The building of an image of Jube:
  * Starting an image of Jube for WebAPI Services (API and User Interface).
  * Starting an image of Jube for the Background Jobs.

Jube is not built to a Docker Hub image, instead an image will be built to the dockerfile specification in the project.

With the prerequisites in place, Jube can be up and running in just a few minutes:

```shell
git clone https://github.com/jube-home/aml-transaction-monitoring
cd jube
export DockerComposePostgresPassword="SuperSecretPasswordToChange"
export DockerComposeJWTKey="IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mk|1oMC50TvAPi%{mUt<9"B&|>DP|GZy"YYWeVrNUqLQE}mz{L_UsingThisKeyIsDangerous"
docker compose up -d
```

Upon conclusion Jube will be listening on the docker host, on port 5001, hence (http://localhost:5001).

# Quickstart with dotnet run

Jube runs on commodity Linux. For running directly, there exists the following prerequisites:

* .Net 9 Runtime.
* Postgres database version 13 onwards (tested on 15.4 but no significant database development to cause a breaking
  change).
* Optional but recommended: Redis version 6 or above (it probably works fine on earlier versions, as the command used
  are basic. RESP
  wire compatible implies that it is possible to use KeyDB, DragonflyDB, Garnet or any RESP compliant wire protocol
  database).

Subject to prerequisites, Jube can be up and running in minutes:

```shell
git clone https://github.com/jube-home/aml-transaction-monitoring
cd jube/Jube.App
export ConnectionString="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=SuperSecretPasswordToChange;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;SSL Mode=Require;Trust Server Certificate=true;"
export RedisConnectionString="localhost"
export ASPNETCORE_URLS="https://localhost:5001"
export JWTKey="IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mk|1oMC50TvAPi%{mUt<9"B&|>DP|GZy"YYWeVrNUqLQE}mz{L_UsingThisKeyIsDangerous"
dotnet run
```

# Quickstart notes

For security, and docker support, there is no means to pass configuration values via anything other than Environment
Variables, and the
contents of those Environment Variables are never - ever - stored by Jube (which is something the CodeQL security
scanner tests for).

The use of Redis is encouraged as it provides a 33% improvement in response times, and a marked improvement in response
time variance contrasted against using Postgres Database. Redis also does not require Cache table indexing jobs, and
while such indexing is automatic on existing data for Postgres, it does create some delay in the creation of Search Keys
retroactively, however by contrast Search Keys in Redis can only be created on a forward only basis and there is no
preexisting data. In general the trade-off between Key \ Value Pair in-memory databases and RDMBS durable databases is
not
trivial. In general, the use of Postgres Database is probably the right choice for low volume or cost sensitive
implementations
where the staff and infrastructure complexity costs can't be justified, whereas for any serious real-time implementation
given infrastructure technical capacity, doubtless Redis is the better choice. Setting the Redis Environment Variable to
false will fall back to using the Postgres Database for cache, and is the more simple implementation:

```shell
export Redis="False"
```

There are sensitive cryptographic values that need to be included at startup. At a minimum the JWTKey value is required:

```shell
export JWTKey="IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mk|1oMC50TvAPi%{mUt<9"B&|>DP|GZy"YYWeVrNUqLQE}mz{L_UsingThisKeyIsDangerous"
```

The JWTKey value is used to encrypt access tokens providing for API authentication, and therefore user interface
authentication.

While outside the scope of this installation documentation, other sensitive variables, while optional, are strongly
suggested:

```shell
export PasswordHashingKey="IMPORTANT:_ChangeThisKey_~%pvif3KRo!3Mk|1oMC50TvAPi%{mUt<9"B&|>DP|GZy"YYWeVrNUqLQE}mz{L_UsingThisKeyIsDangerous"
```

It is imperative that the keys be changed from their defaults and kept safe in appropriate storage. Jube will not start
if the keys above are used.

Change the template value for setting the ConnectionString and JWTKey Environment Variables, then run the shell script
as above. Wait for the build to complete, after which observe the welcome message.

Waiting a few moments more will ensure that the embedded Kestrel web server is started correctly. In a web browser,
navigate to the bound URL [https://localhost:5001/](https://localhost:5001/) as per the ASPNETCORE_URLS Environment
Variable.

The default username \ password combination is Administrator \ Administrator, although the password will be need to be
changed on first login.

A more comprehensive installation guide is available in
the [Getting Started](https://jube-home.github.io/jube/GettingStarted/) of
the [documentation](https://jube-home.github.io/jube).

# Reporting Vulnerabilities

Please do not file GitHub issues for security vulnerabilities, as they are public.

Jube takes security issues very seriously. If you have any concerns about Jube or believe you have uncovered a
vulnerability, please contact via the e-mail address security@jube.io. In the message, try to describe the issue and,
ideally, a way of reproducing it.

Please report any security problems to Jube before disclosing them publicly.

# Governance

Jube Holdings Limited is a Cyprus company registered HE404521. Jube Holdings Limited owns Jube software and Trademarks (
registered or otherwise). Jube is maintained by Jube Operations Limited, a United Kingdom company with registration 14442207.
Jube Operations Limited is a wholly owned subsidiary of Jube Holdings Limited. Jube Operations Limited
provides training and support services for Jube. Jube and "Jooby" (the logo) is a registered trademark in Cyprus.

# Licence

Jube is distributed under [AGPL-3.0-or-later](https://www.gnu.org/licenses/agpl-3.0.txt).