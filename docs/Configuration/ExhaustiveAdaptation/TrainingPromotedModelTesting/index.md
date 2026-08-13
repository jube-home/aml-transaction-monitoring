---
layout: default
title: Exhaustive Adaptation Training Promoted Models Performance
nav_order: 5
parent: Exhaustive Adaptation
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

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