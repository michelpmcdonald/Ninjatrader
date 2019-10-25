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
	public class SessionMid : Indicator
	{
		protected SessionIterator sessionIterator;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "SessionMid";
				BarsRequiredToPlot 							= 0;
				Calculate									= Calculate.OnPriceChange;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(Brushes.LightSeaGreen, "MidPoint");
			}
			else if (State == State.Configure)
			{
			}
			else if (State == State.DataLoaded)
			{
				sessionIterator = new SessionIterator(Bars);
			}
		}

		protected override void OnBarUpdate()
		{
			// Reset for every session
			if (sessionIterator.IsNewSession(Time[0], false))
			{
				sessionIterator.GetNextSession(Time[0], false);
			}
			
			MidPoint[0] = ((CurrentDayOHL().CurrentHigh[0] - CurrentDayOHL().CurrentLow[0]) / 2) + CurrentDayOHL().CurrentLow[0];
			
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
				RemoveDrawObject(Time[0].Date.ToString() + "mid");
				Draw.Line(this, Time[0].Date.ToString() + "mid", true, sessionIterator.ActualSessionBegin, MidPoint[0], Time[0], MidPoint[0], Plots[0].Brush, Plots[0].DashStyleHelper, (int)Plots[0].Pen.Thickness);		
			}	
		}

		#region Properties

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MidPoint
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
		private SessionMid[] cacheSessionMid;
		public SessionMid SessionMid()
		{
			return SessionMid(Input);
		}

		public SessionMid SessionMid(ISeries<double> input)
		{
			if (cacheSessionMid != null)
				for (int idx = 0; idx < cacheSessionMid.Length; idx++)
					if (cacheSessionMid[idx] != null &&  cacheSessionMid[idx].EqualsInput(input))
						return cacheSessionMid[idx];
			return CacheIndicator<SessionMid>(new SessionMid(), input, ref cacheSessionMid);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SessionMid SessionMid()
		{
			return indicator.SessionMid(Input);
		}

		public Indicators.SessionMid SessionMid(ISeries<double> input )
		{
			return indicator.SessionMid(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SessionMid SessionMid()
		{
			return indicator.SessionMid(Input);
		}

		public Indicators.SessionMid SessionMid(ISeries<double> input )
		{
			return indicator.SessionMid(input);
		}
	}
}

#endregion
