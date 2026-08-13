---
layout: default
title: CLI Password Reset
nav_order: 2
parent: CLI
grand_parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

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

Pass the new password in the clear as shown above regardless of the target user's wire hashing scheme - the CLI handles
this transparently. If the user has `WirePasswordHash` enabled (see [Password Transport
Hardening](../../Authentication/index.html#password-transport-hardening)), the tool applies the same SHA-256-with-username
pre-hash used by the browser before Argon2 hashing the result for storage, so the record this writes stays consistent
with however the user would otherwise have set their own password. You do not need to pre-hash the password yourself
either way.

