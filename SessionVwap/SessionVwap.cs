#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
	public class SessionVwap : Indicator
	{
		protected double totalDollars = 0;
		protected double totalShares = 0;
		protected SessionIterator sessionIterator;
		protected Brush curPlotBrush;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				BarsRequiredToPlot 							= 0;
				Description									= @"Session true VWAP.";
				Name										= "SessionVwap";
				Calculate									= Calculate.OnEachTick;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				PrintDebug                                  = false;
				curPlotBrush                                = null;
				AddPlot(Brushes.DarkTurquoise, "Vwap");
			}
			
			else if (State == State.DataLoaded)
			{
				sessionIterator = new SessionIterator(Bars);
			}
			
			else if (State == State.Historical)
			{
				if (Calculate != Calculate.OnEachTick)
					Draw.TextFixed(this, "NinjaScriptInfo", "Tick Data Required for VWAP", TextPosition.BottomRight);
			}
		}

		protected override void OnBarUpdate()
		{
			// Set brush to Transparent on first tick of new session
			// to disconnect indicator plot between sesions.  Plots are
			// relative to current bar and last bar.
			if (Bars.IsFirstBarOfSession && IsFirstTickOfBar)
			{
				PlotBrushes[0][0] = Brushes.Transparent;
			}
			
			// Draw line between start of session and first bar since the 
			// plot is set to transparent for the session disconnect effect 
			if (Bars.IsFirstBarOfSession)
			{
				RemoveDrawObject(Time[0].Date.ToString() + "vwap_start");
				Draw.Line(this, Time[0].Date.ToString() + "vwap_start", true, sessionIterator.ActualSessionBegin, Open[0], Time[0], Vwap[0], Plots[0].Brush, Plots[0].DashStyleHelper, (int)Plots[0].Pen.Thickness);		
			}	
		}
		
		protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
		{
			
			// Workaround for market replay backfilling all of replay sessions data(as historical data)
			// Replay should only backfill history up to replay time, so make sure any historical ticks
			// are before the current replay time.
			if ( (Connection.PlaybackConnection != null) &&
				(State == State.Historical) && 
				(marketDataUpdate.Time >= Connection.PlaybackConnection.Now))
			{
				if (PrintDebug)
				{
					Print("Filter Hit - MarketDataEvent Time :" + marketDataUpdate.Time.ToString() + 
						  " Playback Time: " + Connection.PlaybackConnection.Now.ToString() + 
						  " Event Type: " + marketDataUpdate.MarketDataType + 
						  " State: " + State);
				}
				return;
			}
					
			// Reset for every session
			if (sessionIterator.IsNewSession(marketDataUpdate.Time, false))
			{
				if (PrintDebug)
				{
					Print("NewSession: " + marketDataUpdate.Time.ToString());
				}
				totalShares = 0;
				totalDollars = 0;
				sessionIterator.GetNextSession(marketDataUpdate.Time, false);
			}
			
			// Check session time
            if (!sessionIterator.IsInSession(marketDataUpdate.Time, false, true))
			{
				return;
			}
			
			// Is this a trade(not bid or ask)
  			if (marketDataUpdate.MarketDataType == MarketDataType.Last)
			{
				totalShares += marketDataUpdate.Volume;
				totalDollars += marketDataUpdate.Volume * marketDataUpdate.Price;
				Vwap[0] = totalDollars / totalShares;
				if (PrintDebug)
				{
					Print(marketDataUpdate.Time.ToString() + " Session total shares: " +  totalShares.ToString() + " " + State);
				}
			}
			
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="PrintDebug", Description="Print debug info ", Order=1, GroupName="Parameters")]
		public bool PrintDebug
		{ get; set; }
		
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Vwap
		{
			get { return Values[0]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SessionVwap[] cacheSessionVwap;
		public SessionVwap SessionVwap(bool printDebug)
		{
			return SessionVwap(Input, printDebug);
		}

		public SessionVwap SessionVwap(ISeries<double> input, bool printDebug)
		{
			if (cacheSessionVwap != null)
				for (int idx = 0; idx < cacheSessionVwap.Length; idx++)
					if (cacheSessionVwap[idx] != null && cacheSessionVwap[idx].PrintDebug == printDebug && cacheSessionVwap[idx].EqualsInput(input))
						return cacheSessionVwap[idx];
			return CacheIndicator<SessionVwap>(new SessionVwap(){ PrintDebug = printDebug }, input, ref cacheSessionVwap);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SessionVwap SessionVwap(bool printDebug)
		{
			return indicator.SessionVwap(Input, printDebug);
		}

		public Indicators.SessionVwap SessionVwap(ISeries<double> input , bool printDebug)
		{
			return indicator.SessionVwap(input, printDebug);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SessionVwap SessionVwap(bool printDebug)
		{
			return indicator.SessionVwap(Input, printDebug);
		}

		public Indicators.SessionVwap SessionVwap(ISeries<double> input , bool printDebug)
		{
			return indicator.SessionVwap(input, printDebug);
		}
	}
}

#endregion
