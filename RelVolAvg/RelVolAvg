#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class RelVolAvg : Indicator
	{
		protected SessionIterator sessItr;
		
		// Drawing alpha
		private double volAlpha;
		
		// Datetime of first bar on chart
		protected bool isFirstChartBar = false;
		protected DateTime chartStartTime;
		
		// Holds Volume at time period hist vol for a bar
		protected List<double> histPeriodVol;
		double totalHistPeriodVol;
		double avgHistPeriodVol;
		double histPeriodVolStd;
		
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Average Relative volume at bar time";
				Name										= "RelVolAvg";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				LookBackDays								= 5;
				MaximumBarsLookBack 						= MaximumBarsLookBack.Infinite;
				ShowTransparentPlotsInDataBox 				= true;   
				
				// Raw period Volume
				VolAboveStdBrush  = Brushes.Firebrick;
				VolWithinStdBrush = Brushes.DimGray;
				VolBelowStdBrush  = Brushes.DeepSkyBlue;
				volAlpha           = 100;
				ShowVolumeBars = true;
        		
				// High low std volume hashes
				ShowStdHiLow = true;
				StdHiBrush = Brushes.WhiteSmoke;
				StdLowBrush = Brushes.WhiteSmoke;
				
				// Avg Rel volume bar
				ShowAvgRelVol = true;
				AvgRelVolBrush = Brushes.WhiteSmoke;
			}
			else if (State == State.Configure)
			{
				// Volume Brushes
				AddPlot(new Stroke(Brushes.Orange, 2), PlotStyle.Bar, "Vol");
				Plots[0].AutoWidth = true;
				
				VolAboveStdBrush   = VolAboveStdBrush.Clone();
				VolAboveStdBrush.Opacity = (float)(volAlpha / 100.0);
				VolAboveStdBrush.Freeze();
				
				VolWithinStdBrush  = VolWithinStdBrush.Clone();
				VolWithinStdBrush.Opacity = (float)(volAlpha / 100.0);
				VolWithinStdBrush.Freeze();
				
				VolBelowStdBrush   = VolBelowStdBrush.Clone();
				VolBelowStdBrush.Opacity = (float)(volAlpha / 100.0);
				VolBelowStdBrush.Freeze();
				
				if (ShowStdHiLow)
				{
					AddPlot(new Stroke(StdLowBrush, 2), PlotStyle.Hash, "RelVolStdLow");
					AddPlot(new Stroke(StdHiBrush, 2), PlotStyle.Hash, "RelVolStdHigh");
				}
				else
				{
					AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Hash, "RelVolStdLow");
					AddPlot(new Stroke(Brushes.Transparent, 1), PlotStyle.Hash, "RelVolStdHigh");
				}
				
				if (ShowAvgRelVol)
				{
					AddPlot(new Stroke(AvgRelVolBrush, 2), PlotStyle.Bar, "RelVolAvg");
				}
				else
				{
					AddPlot(new Stroke(Brushes.Transparent, 2), PlotStyle.Bar, "RelVolAvg");
				}
			}
			else if (State == State.DataLoaded)
			{
				// First bar on chart datetime
				isFirstChartBar = true;
				
				sessItr = new SessionIterator(Bars);
			}
			
			else if (State == State.Historical)
			{
				// Only calc on bar close for historical backfill to
				// speed up processing
				Calculate = Calculate.OnBarClose;
			}
			
			else if (State == State.Transition)
			{
				// When changing to real time feed, calc and display
				// indicator for each trade for real time indicator updates
				Calculate = Calculate.OnEachTick;
			}
			
		}
		
		private double getStandardDeviation(List<double> doubleList)  
		{  
   			double average = doubleList.Average();  
   			double sumOfDerivation = 0;  
   			foreach (double value in doubleList)  
   			{  
      			sumOfDerivation += (value) * (value);  
   			}  
   			double sumOfDerivationAverage = sumOfDerivation / (doubleList.Count);  
   			return Math.Sqrt(sumOfDerivationAverage - (average*average));  
		}  

		protected override void OnBarUpdate()
		{
			// Get time of first bar on chart
			if (isFirstChartBar)
			{
				isFirstChartBar = false;
				chartStartTime = Time[0];
			}
			
			// Not Enough History to calc rel vol
			if (Time[0].Date.AddDays(LookBackDays * -1) < chartStartTime.Date)
			{
				return;
			}
			
			// Only gather vol history on first tick
			// of bar since history does not change for
			// a given bar
			if (IsFirstTickOfBar)
			{
				int skipDays = 0;
				totalHistPeriodVol = 0;
				histPeriodVol = new List<double>(LookBackDays);
				
				// Loop until all same timeperiod lookback days 
				// are found or lookback exceeds history
				while (histPeriodVol.Count < LookBackDays)
				{
					// Bar target time
					DateTime tt = Time[0].AddDays((histPeriodVol.Count + 1 + skipDays) * -1);
					
					// Bail if target day is before first chart, not enough history
					if (tt.Date < Time[CurrentBar].Date)
					{
						return;
					}
					
					// No weekends
					if( (tt.DayOfWeek == DayOfWeek.Saturday) || (tt.DayOfWeek == DayOfWeek.Sunday) )
					{
						skipDays++;
						continue;
					}
					
					// Skip over early close days and Holidays
					if (TradingHours.PartialHolidays.ContainsKey(tt.Date) ||
						TradingHours.Holidays.ContainsKey(tt.Date))
					{
						skipDays++;
						continue;
					}
					
					// Get Prev day's same time bar
					int relPrevBar = ChartBars.GetBarIdxByTime(ChartControl, tt);
									
					// If the returned bar is not the expected date, a bar with the
					// timetarget is not on the chart.  Dont know why GetBarIdxByTime
					// does not return something more normal(like a -1) for a GetBarIdxXXX()
					// miss.  Instead it just returns the incorrect bar.
					// At this point a missing bar is unexpected
					if ( tt != Bars.GetTime(relPrevBar))
					{
						throw new Exception("Missing data error - Target Time: " + tt + " Found Time: " + Bars.GetTime(relPrevBar));
					}
					
					totalHistPeriodVol += Bars.GetVolume(relPrevBar);
					histPeriodVol.Add(Bars.GetVolume(relPrevBar));
				}
				histPeriodVolStd = this.getStandardDeviation(histPeriodVol);
				avgHistPeriodVol = totalHistPeriodVol / LookBackDays;
			}
			
			// Get historical average at time volume from last x days
			Values[3][0] = avgHistPeriodVol;
			
			// Difference between avg period vol and current bars vol
			double rel_vol_diff = Volume[0] - avgHistPeriodVol;
			
			// Std Hi-Low Hash marks
			Values[1][0] = Math.Max(0.0, avgHistPeriodVol - histPeriodVolStd);
			Values[2][0] = avgHistPeriodVol + histPeriodVolStd;
			
			// Raw period volume bars, color code based on 
			// volume above, below or within 1 std band
			Values[0][0] = Volume[0];
			if (ShowVolumeBars)
			{
				// Greater than one std
				if(Volume[0] > Values[2][0])
				{
					PlotBrushes[0][0] = VolAboveStdBrush;
				}
				// Within one std
				else if(Values[0][0] > Values[1][0]) 
				{
					PlotBrushes[0][0] = VolWithinStdBrush;
				}
				// Less than 1 std
				else
				{
					PlotBrushes[0][0] = VolBelowStdBrush;
				}
			}
			else
			{
				PlotBrushes[0][0] = Brushes.Transparent;	
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="LookBackDays", Description="Number of period lookback days to include", Order=1, GroupName="Parameters")]
		public int LookBackDays
		{ get; set; }
		
		
		// Volume Bar Properties
		[Display(Name="Visible", Description="Show Volume Bars", Order=0, GroupName="Volume Bars")]
		public bool ShowVolumeBars { get; set; }
		[Display(Name="Above Std", Description="Volume that exceeds RelVol by 1 Standard Deviation", Order=1, GroupName="Volume Bars")]
		public Brush VolAboveStdBrush { get; set; }
		[Display(Name="Within Std", Description="Volume that is within 1 Standard Deviation of RelVol", Order=2, GroupName="Volume Bars")]
		public Brush VolWithinStdBrush { get; set; }
		[Display(Name="Below Std", Description="Volume that is below 1 Standard Deviation of RelVol", Order=3, GroupName="Volume Bars")]
		public Brush VolBelowStdBrush { get; set; }
		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Opacity", Order = 4, GroupName = "Volume Bars")]
		public double VolAlpha
		{
			get { return volAlpha; }
			set { volAlpha = Math.Max(1, value); }
		}
		
		// Std range hi low hash marks
		[Display(Name="Visible", Description="Show 1 Std Hi-Low Hashes", Order=0, GroupName="1 Std Hi-Low")]
		public bool ShowStdHiLow { get; set; }
		[Display(Name="Std Hi", Description="Std High Mark", Order=1, GroupName="1 Std Hi-Low")]
		public Brush StdHiBrush { get; set; }
		[Display(Name="Std Low", Description="Std Low Mark", Order=2, GroupName="1 Std Hi-Low")]
		public Brush StdLowBrush { get; set; }
		
		// Avg rel vol
		[Display(Name="Visible", Description="Show Average Rel Vol", Order=0, GroupName="Average Rel Volume")]
		public bool ShowAvgRelVol { get; set; }
		[Display(Name="Average Rel Vol", Description="Average Rel Vol Brush", Order=1, GroupName="Average Rel Volume")]
		public Brush AvgRelVolBrush { get; set; }
	
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private RelVolAvg[] cacheRelVolAvg;
		public RelVolAvg RelVolAvg(int lookBackDays)
		{
			return RelVolAvg(Input, lookBackDays);
		}

		public RelVolAvg RelVolAvg(ISeries<double> input, int lookBackDays)
		{
			if (cacheRelVolAvg != null)
				for (int idx = 0; idx < cacheRelVolAvg.Length; idx++)
					if (cacheRelVolAvg[idx] != null && cacheRelVolAvg[idx].LookBackDays == lookBackDays && cacheRelVolAvg[idx].EqualsInput(input))
						return cacheRelVolAvg[idx];
			return CacheIndicator<RelVolAvg>(new RelVolAvg(){ LookBackDays = lookBackDays }, input, ref cacheRelVolAvg);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.RelVolAvg RelVolAvg(int lookBackDays)
		{
			return indicator.RelVolAvg(Input, lookBackDays);
		}

		public Indicators.RelVolAvg RelVolAvg(ISeries<double> input , int lookBackDays)
		{
			return indicator.RelVolAvg(input, lookBackDays);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.RelVolAvg RelVolAvg(int lookBackDays)
		{
			return indicator.RelVolAvg(Input, lookBackDays);
		}

		public Indicators.RelVolAvg RelVolAvg(ISeries<double> input , int lookBackDays)
		{
			return indicator.RelVolAvg(input, lookBackDays);
		}
	}
}

#endregion
