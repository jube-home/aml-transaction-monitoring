---
layout: default
title: CLI Password Reset
nav_order: 2
parent: CLI
grand_parent: Concepts
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# CLI Password Reset
In the event of total password lock and unavailability of the application for all administrative users, the password can be reset via the CLI:

``` shell
.Jube.CLI -cs "<Insert Database Connection String>" -urpr <Insert Password Salt> <Insert User Name> <Insert New Password> 
```

The parameters for the function -urpr are as follows:

| Parameter | Description                                 | Example Value             |
|-----------|---------------------------------------------|---------------------------|
| Salt      | The PasswordHashingKey Environment Variable | ExtraSuperSecretRandomKey | 
| User Name | The user name to be reset.                  | Administrator             |
| Password  | The new password to set for the user name.  | StrongPassword            |

As following example (noting the -cs requires the database connection string as parameter which has been wrapped by double quotation given the necessary presence of the space character in the string):

``` shell
.Jube.CLI -cs "Host=127.0.0.1;Database=test;Username=postgres;Password=secret;Pooling=true;Minimum Pool Size=0;Maximum Pool Size=100;" -urpr ExtraSuperSecretRandomKey Administrator StrongPassword
```

