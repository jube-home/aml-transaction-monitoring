---
layout: default
title: Installing .Net 9 Runtime to Virtual Machine (aka Droplet)
nav_order: 4
parent: Getting Started
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Installing .Net 9 Runtime to Virtual Machine (aka Droplet)

The Droplet will be missing the .Net 9 Runtime by default, and it needs to be installed.  The packages are available from Microsoft as follows:

https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu

For Ubuntu 24.10 Linux for the purposes of this tutorial:

https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2410

Create a Droplet:

![Image](ExampleOfLatestVersion.png)

Navigate to cloud bash command line for the Droplet:

![Image](HowToGetToConsole.png)

![Image](LinuxVersion.png)

The command to install can be extracted as follows:

```shell
sudo apt-get update && \
  sudo apt-get install -y aspnetcore-runtime-9.0
```

The shell command above is simply executed in the console via copy and paste:

![Image](CopyAndPasteToBash.png)

Pressing enter will begin the process:

![Image](Working.png)

Until completion:

![Completion.png](Completion.png)