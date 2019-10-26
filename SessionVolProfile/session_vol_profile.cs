using System;
using System.Collections.Generic;

class VolPrice {
  public int BidVol;
  public int AskVol;
}

struct SessionVolProfile {
  public DateTime Start;
  public DateTime End;
  public double Poc;
  public double ValueAreaHigh;
  public double ValueAreaLow;
  public int Vol;
  public SortedList<double, VolPrice> VolPrices;

  public SessionVolProfile(DateTime start, DateTime end) {
    Start = start;
    End = end;
    Poc = Double.MinValue;
    ValueAreaHigh = Double.MinValue;
    ValueAreaLow = Double.MaxValue;
    Vol = 0;
    VolPrices = new SortedList<double, VolPrice>();
  }

  public void AddTrade(double price, int vol, bool atAsk) {
      VolPrice vp;
      
      // Fetch existing or create and add new volPrice level
      if (VolPrices.ContainsKey(price)) {
        vp = VolPrices[price];
      }
      else {
        vp = new VolPrice();
        VolPrices[price] = vp;
      }
      
      
      // Update Bid or ask vol
      if (atAsk) {
          vp.AskVol += vol;
      }
      else {
          vp.BidVol += vol;
      }

      // Update session Vol
      Vol += vol;
    
      // Update Point of control if needed
      if (Poc == Double.MinValue || 
          (vp.BidVol + vp.AskVol) > (VolPrices[Poc].BidVol + VolPrices[Poc].AskVol)) 
      {
        Poc = price;
      }

      // Update Value area
      int targetVol = (int)(Vol * .7);
      int expVol = VolPrices[Poc].BidVol;
      expVol += VolPrices[Poc].AskVol;
      int lowOffset = 2;
      int highOffset = 2;
      int pocIndex = VolPrices.IndexOfKey(Poc);
      ValueAreaHigh = Poc;
      ValueAreaLow = Poc;
      
      // Build up value area volume until the target volume amt is hit
      // or there are no more price levels 
      while (expVol < targetVol) { 
        
        // Get 2 lower prices volume if there are two lower price
        // levels, otherwise ignore
        int lv = 0;
        if (pocIndex - lowOffset >= 0) {
          lv =  VolPrices.Values[pocIndex - lowOffset + 1].BidVol; 
          lv += VolPrices.Values[pocIndex - lowOffset + 1].AskVol;
          lv += VolPrices.Values[pocIndex - lowOffset].BidVol;
          lv += VolPrices.Values[pocIndex - lowOffset].AskVol;
        }
        
        //Get 2 higher prices if there are two higher price
        // levels, otherwise ignore
        int hv = 0;
        if ((pocIndex + highOffset) < VolPrices.Count) {
          hv =  VolPrices.Values[pocIndex + highOffset].BidVol;
          hv += VolPrices.Values[pocIndex + highOffset].AskVol;
          hv += VolPrices.Values[pocIndex + highOffset - 1].BidVol;
          hv += VolPrices.Values[pocIndex + highOffset - 1].AskVol;
        }

        // No pairs left
        if (hv == 0 && lv == 0 ) {
          break;
        }

        // Take price level with most vol 
        if (hv >= lv) {
          expVol += hv;
          ValueAreaHigh = VolPrices.Keys[pocIndex + highOffset];
          highOffset += 2;
        }
        else {
          expVol += lv;
          ValueAreaLow = VolPrices.Keys[pocIndex - lowOffset];
          lowOffset += 2;
        }
      }  
  }
}

struct Trade {
  public double Price;
  public int Vol;
  public bool AtAsk;

  public Trade(double price, int vol, bool atAsk) {
    Price = price;
    Vol = vol;
    AtAsk = atAsk;
  }
}

class MainClass {
  public static void Main (string[] args) {
    Console.WriteLine ("Hello World");

    DateTime start =  new DateTime(2019, 10, 9, 9, 30, 0); 
    DateTime end = new DateTime(2019, 10, 9, 16, 15, 0); 
    SessionVolProfile svp = new SessionVolProfile(start, end);
    
    List<Trade> trades = new List<Trade>();
    trades.Add(new Trade(2899.75, 5, true));
    trades.Add(new Trade(2900.00, 5, true));
    trades.Add(new Trade(2900.25, 5, true));
    trades.Add(new Trade(2900.50, 5, false));
    trades.Add(new Trade(2900.75, 50, true));
    trades.Add(new Trade(2901.00, 30, true));
    

    foreach (Trade trd in trades) {
      svp.AddTrade(trd.Price, trd.Vol, trd.AtAsk);
    }
  Console.WriteLine("POC: " + svp.Poc.ToString());
  Console.WriteLine("VA Low: " + svp.ValueAreaLow.ToString());
  Console.WriteLine("VA High:" + svp.ValueAreaHigh.ToString());
  }
}
