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
	public class SessionVolumeProfile : Indicator
	{
		protected SessionIterator sessItr;
		
		// Holds each chart sessions vol profile
		protected List<VolProfile> volProfiles;
		
		// Holds the current session's vol profile
		protected VolProfile curSesVolProfile;
		
		// X Chart width of Session Profile drawing as a %
		//  of the X chart width of the entire session
		protected int sessionWidth;
		
		// Current top of book, abstracts diff between replay and live
		private double askPrice;
		private double bidPrice;
		
		// Drawing alpha
		private double alpha;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description					= @"Session Volume Profile";
				Name						= "SessionVolumeProfile";
				Calculate					= Calculate.OnEachTick;
				IsChartOnly					= true;
				IsOverlay					= true;
				DrawOnPricePanel			= false;
				PrintDebug                  = false;
				sessionWidth                = 90;
				EnableValueArea             = true;
				VpBrush                     = Brushes.SlateGray;
				VaBrush                     = Brushes.Black;
				alpha                       = 50;
				
			}
			else if (State == State.Configure)
			{
				ZOrder = -1;
			}
			else if (State == State.DataLoaded)
			{
				sessItr = new SessionIterator(Bars);
				volProfiles = new List<VolProfile>();
			}
			else if (State == State.Historical)
			{
				if (Calculate != Calculate.OnEachTick)
					Draw.TextFixed(this, "NinjaScriptInfo", string.Format(NinjaTrader.Custom.Resource.NinjaScriptOnBarCloseError, Name), TextPosition.BottomRight);
			}
		}

		protected override void OnMarketData(MarketDataEventArgs mde)
		{
			// Workaround for market replay backfilling all of replay sessions data(as historical data)
			// Replay should only backfill history up to replay time, so make sure any historical ticks
			// are before the current replay time.  This issue seems to be fixed in NT 8 build 19.1, so
			// remove this code at some point
			if ( (Connection.PlaybackConnection != null) &&
				(State == State.Historical) && 
				(mde.Time >= Connection.PlaybackConnection.Now))
			{
				if (PrintDebug)
				{
					Print("Filter Hit - MarketDataEvent Time :" + mde.Time.ToString() + 
						  " Playback Time: " + Connection.PlaybackConnection.Now.ToString() + 
						  " Event Type: " + mde.MarketDataType + 
						  " State: " + State);
				}
				return;
			}
			
			// Reset for every session
			if (sessItr.IsNewSession(mde.Time, false))
			{
				// Debug Info 
				if (PrintDebug)
				{
					Print("NewSession: " + mde.Time.ToString());
				}
				
				sessItr.GetNextSession(mde.Time, false);
				
				// Create new session vol profle
				curSesVolProfile = new VolProfile(sessItr.ActualSessionBegin, sessItr.ActualSessionEnd);
				volProfiles.Add(curSesVolProfile);
			}
			
			// Check session time
            if (!sessItr.IsInSession(mde.Time, false, true))
			{
				return;
			}
			
			//Process trades
			if (Bars.IsTickReplay)
			{
				if (mde.MarketDataType == MarketDataType.Last)
				{
					curSesVolProfile.AddTrade(mde.Price, mde.Volume, mde.Price >= mde.Ask); 
				}
			}
			else
			{
				if (mde.MarketDataType == MarketDataType.Ask)
				{
					askPrice = mde.Price;
					return;
				}

				if (mde.MarketDataType == MarketDataType.Bid)
				{
					bidPrice = mde.Price;
					return;
				}

				if (mde.MarketDataType != MarketDataType.Last || ChartControl == null || askPrice == 0 || bidPrice == 0)
					return;

				if (Bars != null && !sessItr.IsInSession(Core.Globals.Now, true, true))
					return;

				curSesVolProfile.AddTrade(mde.Price, mde.Volume, mde.Price >= askPrice);
			}
		} // End of OnMarketData

		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
			if(Bars == null || Bars.Instrument == null || IsInHitTest)
				return;
			
			double	tickSize = Bars.Instrument.MasterInstrument.TickSize;
			
			SharpDX.Direct2D1.Brush barBrush = VpBrush.ToDxBrush(RenderTarget);
			barBrush.Opacity =  (float)(alpha / 100.0);
			
			SharpDX.Direct2D1.Brush vaBrush  = VaBrush.ToDxBrush(RenderTarget);
			vaBrush.Opacity =  (float)(alpha / 100.0);
			
			SharpDX.Direct2D1.Brush drawBrush;
			
			
			//For each session vol profile
			foreach (VolProfile vp in volProfiles) 
			{
				// Get session chart xpos and width
				float xPos = chartControl.GetXByTime(vp.Start);
				float sessWidth = ((chartControl.GetXByTime(vp.End) - xPos) * (sessionWidth / (float)100.0));
				
				// Draw each price level in session
				foreach (KeyValuePair<double, VolPrice> pl in vp.VolPrices)
				{
					// Price level values
					double vpPrice = pl.Key;
					long   vpVol   = pl.Value.Vol;
					
					// Get chart Y
					double	priceLower			= vpPrice - tickSize / 2;
					float	yLower				= chartScale.GetYByValue(priceLower);
					float	yUpper				= chartScale.GetYByValue(priceLower + tickSize);
					float	height				= Math.Max(1, Math.Abs(yUpper - yLower) - 1);
					
					// Draw bars in Value area with Value Area Brush
					if ( EnableValueArea && (vpPrice >= vp.ValueAreaLow) && (vpPrice <= vp.ValueAreaHigh) )
					{
						drawBrush = vaBrush;
					}
					else
					{
						drawBrush = barBrush;
					}
						
					// Bar width 
					float barWidth = ((float)vpVol / vp.VolPrices[vp.Poc].Vol) * sessWidth;
					
					// Draw the bar
					RenderTarget.FillRectangle(new SharpDX.RectangleF(xPos, yUpper, barWidth, height), drawBrush);
				}
			}
			barBrush.Dispose();
			vaBrush.Dispose();

		} // OnRender(..)
		
		#region Properties
		
		[Range(10, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "SessionWidth", Order = 0, GroupName = "Parameters")]
		public int SessionWidth
		{
			get { return sessionWidth; }
			set { sessionWidth = value; }
		}
		[Display(ResourceType = typeof(Custom.Resource), Name = "Vp Bar Color", Order = 1, GroupName = "Parameters")]
		public Brush VpBrush { get; set; }
		[Display(Name="PrintDebug", Description="Print debug info ", Order=1, GroupName="Parameters")]
		public bool PrintDebug
		{ get; set; }
		[Range(0, 100)]
		[Display(ResourceType = typeof(Custom.Resource), Name = "Opacity", Order = 2, GroupName = "Parameters")]
		public double Opacity
		{
			get { return alpha; }
			set { alpha = Math.Max(1, value); }
		}
		
		
		[Display(Name="ValueArea", Description="Display Value Area Bar color ", Order=0, GroupName="ValueArea")]
		public bool EnableValueArea
		{ get; set; }
		[Display(ResourceType = typeof(Custom.Resource), Name = "Va Bar Color", Order = 1, GroupName = "ValueArea")]
		public Brush VaBrush { get; set; }
		
		
		#endregion
		
		#region Vol Profile Container
		public class VolPrice 
		{
			public long BidVol;
		  	public long AskVol;
			
			public long Vol
			{
				get { return BidVol + AskVol; }
			}
		}

		// Holds trade volume at price levels for a timespan
		public class VolProfile 
		{
			public DateTime Start;
			public DateTime End;
			public double Poc;
		  	public double ValueAreaHigh;
		  	public double ValueAreaLow;
		  	public long Vol;
		  	public SortedList<double, VolPrice> VolPrices;

		  	public VolProfile(DateTime start, DateTime end) 
			{
		    	Start = start;
		    	End = end;
		    	Poc = Double.MinValue;
		    	ValueAreaHigh = Double.MinValue;
		    	ValueAreaLow = Double.MaxValue;
		    	Vol = 0;
		    	VolPrices = new SortedList<double, VolPrice>();
		  	}

		  	public void AddTrade(double price, long vol, bool atAsk) 
			{
		    	VolPrice vp;
		      
		      	// Fetch existing or create and add new volPrice level
		      	if (VolPrices.ContainsKey(price)) 
				{
		        	vp = VolPrices[price];
		      	}
		      	else 
				{
		        	vp = new VolPrice();
		        	VolPrices[price] = vp;
		      	}
		      
		      	// Update Bid or ask vol
		      	if (atAsk) 
				{
		        	vp.AskVol += vol;
		      	}
		      	else 
				{
		        	vp.BidVol += vol;
		      	}

		      	// Update session Vol
		      	Vol += vol;
		    
		      	// Update Point of control if needed
		      	if ((Poc == Double.MinValue) || ((vp.BidVol + vp.AskVol) > (VolPrices[Poc].BidVol + VolPrices[Poc].AskVol)))
		      	{
		        	Poc = price;
		      	}

		      	// Update Value area
		      	int targetVol = (int)(Vol * .7);
		      	long expVol = VolPrices[Poc].BidVol;
		      	expVol += VolPrices[Poc].AskVol;
		      	int lowOffset = 2;
		      	int highOffset = 2;
		      	int pocIndex = VolPrices.IndexOfKey(Poc);
		      	ValueAreaHigh = Poc;
		      	ValueAreaLow = Poc;
		      
		      	// Build up value area volume until the target volume amt is hit
		      	// or there are no more price levels 
		      	while (expVol < targetVol) 
				{ 
		        
		        	// Get 2 lower prices volume if there are
		        	// two lower price levels, otherwise ignore
		        	long lv = 0;
		        	if (pocIndex - lowOffset >= 0) {
		          		lv =  VolPrices.Values[pocIndex - lowOffset + 1].BidVol; 
		          		lv += VolPrices.Values[pocIndex - lowOffset + 1].AskVol;
		          		lv += VolPrices.Values[pocIndex - lowOffset].BidVol;
		          		lv += VolPrices.Values[pocIndex - lowOffset].AskVol;
		        	}
		        
		        	// Get 2 higher prices if there are two higher price
		        	// levels, otherwise ignore
			        long hv = 0;
			        if ((pocIndex + highOffset) < VolPrices.Count) 
					{
			        	hv =  VolPrices.Values[pocIndex + highOffset].BidVol;
			         	hv += VolPrices.Values[pocIndex + highOffset].AskVol;
			         	hv += VolPrices.Values[pocIndex + highOffset - 1].BidVol;
			          	hv += VolPrices.Values[pocIndex + highOffset - 1].AskVol;
			        }

		        	// No pairs left
		        	if (hv == 0 && lv == 0 ) 
					{
		          		break;
		        	}

		        	// Take price level with most vol 
		        	if (hv >= lv) 
					{
		          		expVol += hv;
		          		ValueAreaHigh = VolPrices.Keys[pocIndex + highOffset];
		          		highOffset += 2;
		        	}
		        	else 
					{
		          		expVol += lv;
		          		ValueAreaLow = VolPrices.Keys[pocIndex - lowOffset];
		          		lowOffset += 2;
		        	}
		      	}  
		  	}
		}

	#endregion
		
		
	} //End of Indicator
	

	
		
		
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private SessionVolumeProfile[] cacheSessionVolumeProfile;
		public SessionVolumeProfile SessionVolumeProfile()
		{
			return SessionVolumeProfile(Input);
		}

		public SessionVolumeProfile SessionVolumeProfile(ISeries<double> input)
		{
			if (cacheSessionVolumeProfile != null)
				for (int idx = 0; idx < cacheSessionVolumeProfile.Length; idx++)
					if (cacheSessionVolumeProfile[idx] != null &&  cacheSessionVolumeProfile[idx].EqualsInput(input))
						return cacheSessionVolumeProfile[idx];
			return CacheIndicator<SessionVolumeProfile>(new SessionVolumeProfile(), input, ref cacheSessionVolumeProfile);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.SessionVolumeProfile SessionVolumeProfile()
		{
			return indicator.SessionVolumeProfile(Input);
		}

		public Indicators.SessionVolumeProfile SessionVolumeProfile(ISeries<double> input )
		{
			return indicator.SessionVolumeProfile(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.SessionVolumeProfile SessionVolumeProfile()
		{
			return indicator.SessionVolumeProfile(Input);
		}

		public Indicators.SessionVolumeProfile SessionVolumeProfile(ISeries<double> input )
		{
			return indicator.SessionVolumeProfile(input);
		}
	}
}

#endregion
