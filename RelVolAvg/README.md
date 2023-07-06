# Average Relative Volume Indicator : Ninjatrader 8 Indicator that displays average volume at bar time period
![Opening Range left session Opening range complete, right session is in progress](img/rel_vol.png)
Wide colored bars are bar's volume, color coded against average relative volume standard deviation.  In Image:
#### Red Bar - 1 standard deviation above average relative volume
#### Grey Bar - Within 1 standard deviation of average relative volume
#### Blue Bar - 1 standard deviation below average relative volume

#### White horizontal hash marks - +/- 1 standard deviation of relative volume average
#### White vertical line - average relative volume

I've had a few people with issues of this indicator "crashing"\throwing an exception.  Usually the issue is missing historic bars.  

I "detect" them and throw an exception:

So, for example, for relvol on a 15 minute chart for the bar starting at 9:30, I go back in "history" and try to grab the 9:30 bars for X previous days.....lets say I can't find the 9:30 bar for 2 days ago, i throw an exception.

I have a few ideas on how to deal with a missing history bar...like for example:
Just skip it, so if your relvol period is 5 days, and your missing day 3, just calc the average period volume for 4 days.
Or average the bar before and the bar after the missing bar and use that for the missing bars volume.

Here is a link to where I throw an exception for missing history, so that would be the spot in the code to start to deal with it if you want to give it a go:
Ninjatrader/RelVolAvg/RelVolAvg.cs

Line 211 in caa3c21

 // If the returned bar is not the expected date, a bar with the 

## Author

Michel McDonald
