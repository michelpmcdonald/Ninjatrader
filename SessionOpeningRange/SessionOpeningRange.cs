// Opening range specified in seconds after session open.
// Works with any intraday timeframe, but requires tick data

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
	public class SessionOpeningRange : Indicator
	{
		protected SessionIterator sessionIterator;
		
		protected double orHigh;
		protected DateTime orHighDateTime;
		protected double orLow;
		protected DateTime orLowDateTime;
		protected bool rendered = false;
		protected DateTime renderEndTime;
		protected DateTime orCutoffTime;
		protected bool orComplete;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				BarsRequiredToPlot 							= 0;
				Description									= @"High and low of first x seconds of day";
				Name										= "SessionOpeningRange";
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				CutoffSeconds                               = 30;
				
				HighBrush = Brushes.Yellow;
				HighBrushWidth = 2;
				LowBrush = Brushes.Yellow;
				LowBrushWidth = 2;
				
				// The or high low lines on the chart are not plots, just lines
				// Adding transparent plot values to display high low values in databox
				AddPlot(Brushes.Transparent, "OrHigh");
				AddPlot(Brushes.Transparent, "OrLow");
				ArePlotsConfigurable = false;
				ShowTransparentPlotsInDataBox = true;  
			}
						
			else if (State == State.DataLoaded)
			{
				sessionIterator = new SessionIterator(Bars);	
			}
			
		}
		
		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			// Workaround for market replay sending out all of replay sessions data(as historical data)
			// Replay should only backfill history up to replay time, so make sure any historical ticks
			// are before the current replay time.
			// this code to OnBar and set for on price change notifications for better performance once
			// this bug is resolved
			if ( (Connection.PlaybackConnection != null) &&
				(State == State.Historical) && 
				(marketDataUpdate.Time >= Connection.PlaybackConnection.Now))
			{
				return;
			}
			
			bool redrawHigh = false;
			bool redrawLow = false;
			
			// Reset for every session
			if (sessionIterator.IsNewSession(marketDataUpdate.Time, true))
			{
				orHigh = Double.MinValue;
				orLow = Double.MaxValue;
				sessionIterator.GetNextSession(marketDataUpdate.Time, true);
				renderEndTime = sessionIterator.ActualSessionEnd;
				orCutoffTime = sessionIterator.ActualSessionBegin.AddSeconds(CutoffSeconds);
				rendered = false;
				orComplete = false;
			}
			
			if(orComplete)
			{
				return;
			}
			
			// Check session time
            if (!sessionIterator.IsInSession(marketDataUpdate.Time, false, true))
			{
				return;
			}
						
			// keep track of high low
			if (marketDataUpdate.MarketDataType == MarketDataType.Last)
			{
				if (!orComplete && (marketDataUpdate.Time <= orCutoffTime))
				{
					redrawHigh = false;
					if (marketDataUpdate.Price > orHigh)
					{
						orHigh = marketDataUpdate.Price;
						orHighDateTime = marketDataUpdate.Time;
						redrawHigh = true;
						Values[0][0] = orHigh;
					}
					
					redrawLow = false;
					if (marketDataUpdate.Price < orLow)
					{
						orLow = marketDataUpdate.Price;
						orLowDateTime = marketDataUpdate.Time;
						redrawLow = true;
						Values[1][0] = orLow;
					}
					
					if (redrawHigh) 
					{
						RemoveDrawObject(marketDataUpdate.Time.Date.ToString() + "or_high");
						Draw.Line(this, marketDataUpdate.Time.Date.ToString() + "or_high", true, orHighDateTime, orHigh, renderEndTime, orHigh, HighBrush, DashStyleHelper.Dot, HighBrushWidth);		
					}
					
					if (redrawLow) 
					{
						RemoveDrawObject(marketDataUpdate.Time.Date.ToString() + "or_low");	
						Draw.Line(this, marketDataUpdate.Time.Date.ToString() + "or_low", true, orLowDateTime, orLow, renderEndTime, orLow, LowBrush, DashStyleHelper.Dot, LowBrushWidth);
					}	
				}
							
				// On first tick past cutoff time mark opening range calc complete and
				// draw the hi and low lines across the entire sesson.
				if((marketDataUpdate.Time > orCutoffTime) && !orComplete) 
				{
					orComplete = true;
					RemoveDrawObject(marketDataUpdate.Time.Date.ToString() + "or_high");
					Draw.Line(this, marketDataUpdate.Time.Date.ToString() + "or_high", true, orHighDateTime, orHigh, renderEndTime, orHigh, HighBrush, DashStyleHelper.Solid, HighBrushWidth);
					RemoveDrawObject(marketDataUpdate.Time.Date.ToString() + "or_low");
					Draw.Line(this, marketDataUpdate.Time.Date.ToString() + "or_low", true, orLowDateTime, orLow, renderEndTime, orLow, LowBrush, DashStyleHelper.Solid, LowBrushWidth);
					
					// Go back to first bar and Set all the high and low plot 
					// values for the day so databox works for indicator
					for (int x = 0; x < Bars.BarsSinceNewTradingDay; x++)
					{
						Values[0][x] = orHigh;
						Values[1][x] = orLow;	
					}
				}
			}
		}
		
		protected override void OnBarUpdate()
		{
			// Set all the high and low plot 
			// values for the day so databox works for indicator
			if (orComplete && IsFirstTickOfBar && Bars.BarsSinceNewTradingDay > 0)
			{
				Values[0][1] = orHigh;
				Values[1][1] = orLow;	
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="CutoffSeconds", Description="Opening Range seconds after open ", Order=1, GroupName="Parameters")]
		public int CutoffSeconds
		{ get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Color", Order = 1, GroupName = "Or High Style")]
		public Brush HighBrush { get; set; }
		
		[Browsable(false)]
		public string HighBrushSerialize
		{
			get { return Serialize.BrushToString(HighBrush); }
  			set { HighBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Width", Order = 2, GroupName = "Or High Style")]
		public int HighBrushWidth { get; set; }
		
		[XmlIgnore]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Color", Order = 1, GroupName = "Or Low Style")]
		public Brush LowBrush { get; set; }
		[Browsable(false)]
		public string LowBrushSerialize
		{
			get { return Serialize.BrushToString(LowBrush); }
  			set { LowBrush = Serialize.StringToBrush(value); }
		}
		
		[Display(ResourceType = typeof(Custom.Resource), Name = "Width", Order = 2, GroupName = "Or Low Style")]
		public int LowBrushWidth { get; set; }
		

		// Provide property to access hi low externally
		[XmlIgnore]
		[Browsable(false)]
		public double OrHigh {
			get 
			{
	      		return orHigh;
	   		}
		}
		[XmlIgnore]
		[Browsable(false)]
		public double OrLow {
			get 
			{
	      		return orLow;
	   		}
		}
		
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SessionOpeningRange[] cacheSessionOpeningRange;
		public SessionOpeningRange SessionOpeningRange(int cutoffSeconds)
		{
			return SessionOpeningRange(Input, cutoffSeconds);
		}

		public SessionOpeningRange SessionOpeningRange(ISeries<double> input, int cutoffSeconds)
		{
			if (cacheSessionOpeningRange != null)
				for (int idx = 0; idx < cacheSessionOpeningRange.Length; idx++)
					if (cacheSessionOpeningRange[idx] != null && cacheSessionOpeningRange[idx].CutoffSeconds == cutoffSeconds && cacheSessionOpeningRange[idx].EqualsInput(input))
						return cacheSessionOpeningRange[idx];
			return CacheIndicator<SessionOpeningRange>(new SessionOpeningRange(){ CutoffSeconds = cutoffSeconds }, input, ref cacheSessionOpeningRange);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SessionOpeningRange SessionOpeningRange(int cutoffSeconds)
		{
			return indicator.SessionOpeningRange(Input, cutoffSeconds);
		}

		public Indicators.SessionOpeningRange SessionOpeningRange(ISeries<double> input , int cutoffSeconds)
		{
			return indicator.SessionOpeningRange(input, cutoffSeconds);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SessionOpeningRange SessionOpeningRange(int cutoffSeconds)
		{
			return indicator.SessionOpeningRange(Input, cutoffSeconds);
		}

		public Indicators.SessionOpeningRange SessionOpeningRange(ISeries<double> input , int cutoffSeconds)
		{
			return indicator.SessionOpeningRange(input, cutoffSeconds);
		}
	}
}

#endregion
