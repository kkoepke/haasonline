// The reference files (found in Haasbot installation folder):
//  - TradeServer.ScriptingDriver.dll
//  - TradeServer.Interfaces.dll
//  - TA-Lib-Core.dll
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TradeServer.ScriptingDriver.DataObjects;
using TradeServer.ScriptingDriver.Interfaces;
using TicTacTec.TA.Library;

public class ehlersMamaFama : IIndicatorScript
{
    //MAMA Parameters:
    double fastLimit = 0.5;
    double slowLimit = 0.05;

    double mama;
    double fama;

    public void Init()
    {
        // In this code example we do not have to do anything
    }

    public string Name { 
        get { return "Script: MAMA/FAMA"; } 
    }

    public int TicksBack
    {
        get { return Core.MamaLookback(fastLimit, slowLimit); }
    }

    public List<ScriptParameter> GetParameters()
    {
        var res = new List<ScriptParameter>
        {
            new ScriptParameter("Fast Limit", ScriptParameterType.Decimal, fastLimit.ToString()),
            new ScriptParameter("Slow Limit", ScriptParameterType.Decimal, slowLimit.ToString())
        };
        return res;
    }

    public void SetParameters(Dictionary<string, object> parameters)
    {
        fastLimit = ParseDouble(parameters["Fast Limit"], fastLimit);
        slowLimit = ParseDouble(parameters["Slow Limit"], slowLimit);
    }

    public List<ChartRecord> GetChartLines()
    {
        List<ChartRecord> chartLines = new List<ChartRecord>
        {
            new ChartRecord() { ChartIndex = 0, LineType = 1, Name = "MAMA", HexLineColor = "#88FF88" },
            new ChartRecord() { ChartIndex = 0, LineType = 1, Name = "FAMA", HexLineColor = "#FF8888" }
        };

        return chartLines;
    }

    public List<decimal> GetChartData()
    {
        List<decimal> chartData = new List<decimal>
            {
                Convert.ToDecimal(mama),
                Convert.ToDecimal(fama)
                //Convert.ToDecimal(DC_delta)
            };

        return chartData;
    }

    public IndicatorResult GetResult(IndicatorContext context) {
        try {
            IPriceInstrument instrument = context.PriceInstrument; // Its pre-created for speed based on the "TicksBack" property field
            int position = instrument.Close.Count() - (1 + context.Offset);
            int outBegIdx, outNBElement;
            double[] outMAMA = new double[position];
            double[] outFAMA = new double[position];

            double[] hl2 = new double[position];
            //double[] hlc3 = new double[position];
            //double[] ohlc4 = new double[position];
            for (int i = 0; i < position; i++)
            {
                hl2[i] = (instrument.High[i] + instrument.Low[i])/2;
                //hlc3[i] = (instrument.High[i] + instrument.Low[i] + instrument.Close[i]) / 3;
                //ohlc4[i] = (instrument.Open[i] + instrument.High[i] + instrument.Low[i] + instrument.Close[i]) / 4;
            }

            var mamaReturnCode = Core.Mama(0, position - 1, hl2, fastLimit, slowLimit, out outBegIdx, out outNBElement, outMAMA, outFAMA );
            //context.Logger.LogToFile("outNBElement: " + outNBElement); //Log to file

            if (mamaReturnCode == Core.RetCode.Success)
            {
                if (outNBElement == 0)
                {
                    // Something went wrong
                    mama = instrument.Close[position - 1];
                    fama = instrument.Close[position - 1];
                    return IndicatorResult.StayNoPosition;
                }
                else
                {
                    mama = outMAMA[outNBElement - 1]; //Take current MAMA (last one of the valid values)
                    fama = outFAMA[outNBElement - 1]; //Take current FAMA (last one of the valid values)

                    //context.Logger.LogToFile("MAMA: " + mama + "\tFAMA:" + fama); //Log to file


                    //Determine indicator signal
                    if (mama > fama)
                    {
                        //context.Logger.LogToFile("Buying at " + instrument.Close + "\tMAMA: " + mama + "\tFAMA:" + fama); //Log to file
                        return IndicatorResult.BuyLong;
                    }
                    else if (fama > mama)
                    {
                        //context.Logger.LogToFile("Selling at " + instrument.Close + "\tMAMA: " + mama + "\tFAMA:" + fama); //Log to file
                        return IndicatorResult.SellShort;
                    }
                    else
                    {
                        return IndicatorResult.StayNoPosition;
                    }
                }
            }
        }
        catch (Exception ex) // This try catch block is an highly recommend practice
        {
            context.Logger.LogToFile(string.Format("Exception detected: {0}", ex.Message));
        }

        return IndicatorResult.StayNoPosition; // Default output
    }

    int ParseInt(object value, int lastValue)
    {
        try
        {
            return Convert.ToInt32(value.ToString());
        }
        catch
        {
            return lastValue;
        }
    }

    double ParseDouble(object value, double lastValue)
    {
        var numStyle = System.Globalization.NumberStyles.Any;
        var cult = System.Globalization.CultureInfo.InvariantCulture;

        try
        {
            double output;
            double.TryParse(
                value.ToString().Replace(',', '.'),
                numStyle,
                cult,
                out output);
            return output;
        }
        catch
        {
            return lastValue;
        }
    }

    string ParseString(object value, string lastValue)
    {
        try
        {
            return Convert.ToString(value);
        }
        catch
        {
            return lastValue;
        }
    }
}
