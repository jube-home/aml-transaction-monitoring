---
layout: default
title: Stopping Training
nav_order: 6
parent: Exhaustive Adaptation
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

Exhaustive model training can take a very long time, and it is computationally expensive. To stop an Exhaustive training
instance identify the Stop button:

![LocationOfStopButton.png](LocationOfStopButton.png)

On clicking the Stop button the training status is set to Stopping:

![Stopping.png](Stopping.png)

The Exhaustive training thread updates the status to signal progress. Before each update of a status by the Exhaustive
training thread,
the same status is checked to see if it is in a Stopping state. In the event that the status is in a Stopping state, the
state is finalised to Stop instead, and the Exhaustive training thread gracefully returned uncompleted. It is worth
noting
that the task in hand will be completed and the thread is not interrupted or aborted, hence it can still be a moderately
time-consuming process to reach a consistent stopping point. The thread is immediately available for the next Exhaustive
training instance.

Once the stopping point has been reached, the status is finalised to Stopped:

![Stopped.png](Stopped.png)



