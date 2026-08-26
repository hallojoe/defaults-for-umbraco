# What are we trying to model?
A simple load-balanced Umbraco setup:


<div class="grid grid-cols-2 gap-8 my-6 w-full">

<div>

Delivery / CD  
https://cd.dev.localhost

- One or more instances
- Serves public traffic
- Scales out by adding instances

</div>
<div>


Backoffice / CM  
https://cm.dev.localhost

- Single instance
- Used by editors
- Scales up by adding resources to the machine
- Responsible for management and scheduled work

</div></div>

The goal is not just to make Umbraco run. The goal is to make the local topology resemble the real topology.


