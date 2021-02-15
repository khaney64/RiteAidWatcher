# RiteAidWatcher
some code to watch stores in specific zip code for open slots for covid vaccines

usage:
  RiteAidWatcher zip [filter=true [maxmiles=999]]

where
- zip is the zip code to "center" the search on
- filter determines if filters should be applied (currently hard coded to exclude philadelphia and non PA states)
- maxm miles lets you limit the search radius - defaults to 999

Code will take the zip code and will then build out a list of stores from that zip code, and expand out from those zip codes up to a list of up to 60 stores (hard coded for now).

Checks for open slots at each of those stores at a reasonable rate.

Currently limited to the first check which looks for slots, which is not perfect (it may incorrection report available slots).  The web site will do the same thing, letting you click the next button, but the next page will show "Apologies, due to high demand, there are currently no appointment times available at this Rite Aid. Please select a different store or check again another day." It may even show some calendar dates on this page, but generally it seems if you see that message there aren't any available.

There is an additional check that could be made, but currently that requires a "moment" parameter and a capcha code, which will require some more complicated loading of pages and pulling those fields from the results.
