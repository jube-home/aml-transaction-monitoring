---
layout: default
title: Exhaustive Adaptation Training Promoted Models Performance
nav_order: 5
parent: Exhaustive Adaptation
grand_parent: Configuration
---

🚀 Speed up implementation with hands-on, face-to-face [training](https://www.jube.io/jube-training) from the developer.
💬 Join the [Jube WhatsApp Public Support Group](https://whatsapp.com/channel/0029Vb7HM7yICVfihDH17H2P) to chat with the
developer.

# Exhaustive Adaptation Training Promoted Models Performance
Promoted models can be manually tested via the Promoted Models Testing tab:

![LocationOfTesting](LocationOfTesting.png)

Clicking on the tab will present the model variables as sliders:

![Sliders](Sliders.png)

The sliders are set to their optimal prescription values for the purpose of classifying positive:

![DefaultValues.png](DefaultValues.png)

Moving the sliders will present the collated values of all sliders to the model to return a score:

![Score.png](Score.png)

The higher the score,  the more likely the simulation is a positive case (e.g. fraud).

The intention of the manual simulation is to process several intuitive scenarios and observe the score to change equally intuitively, although keeping in mind that the model is largely bias to anomaly.

Given a score behaving counter intuitively or not flexibly enough,  it can of be deactivated, and the next highest priority score will be considered promoted.