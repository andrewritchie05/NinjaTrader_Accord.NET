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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;

using Accord;
using Accord.MachineLearning;
using Accord.MachineLearning.DecisionTrees;
using Accord.Math;
using Accord.Math.Random;
using Accord.Statistics;
using Accord.Statistics.Analysis;
using Accord.Statistics.Filters;

#endregion

//This namespace holds Strategies in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Strategies
{
	public class AccordRFexample : Strategy
	{
		private static Series<double> myDir;
		
		private static double[][] z = new double[100][];
		private static int[] Label = new int[100];
		private static double[][] x = new double[1][];
		
		private int false_neg;
		private int false_pos;
		private int answers;
		
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "AccordRFexample";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.Infinite; //.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				
			}
			else if (State == State.Configure)
			{
				SetProfitTarget(CalculationMode.Ticks, 10);
            	SetStopLoss("",CalculationMode.Ticks, 10, false);
				
			}
			else if (State == State.DataLoaded)
			{
				myDir = new Series<double>(this, MaximumBarsLookBack.Infinite); //.TwoHundredFiftySix);
				
				x[0] = new double[5];
				
				for (int i = 0; i < 100; i++)
				{
					z[i] = new double[5];
				}
			} 
		}
		
		//private static double[] NormalizeData(double[] data, int min, int max)
		//{
    	//	var sorted = data.OrderBy(d => d);
    	//	double dataMax = sorted.First();
    	//	double dataMin = sorted.Last();
    	//	double[] ret = new double[data.Length];

    	//	double avgIn = (double)((min + max) / 2.0);
    	//	double avgOut = ((dataMax + dataMin) / 2.0);

    	//	for (int i = 0; i < data.Length; i++)
    	//	{
       	//		ret[i] = (double) Math.Round(avgOut * (data[i] + avgIn) / 2);
    	//	}

    	//	return ret;
		//}
		
		// train classifier and enter trades based on predictions
		private void Classifier()
        {
			Generator.Seed  = 1 ;
			
            // input columns
            double[][] inputs  =  z;// result; 
			
			// normalize the inputs array
			//for (int i = 0; i < 5; i++)
			//{	
			//	z[i] = NormalizeData(z[i], 0, 1);
			//}
            
			// output column
            int[] outputs =   Label; 

			// set the learning algorithm		
			var teacher = new RandomForestLearning()
            {
                NumberOfTrees = 1000,
				//SampleRatio = 1.0,
				//Join = 2,
				//CoverageRatio = 0.5,
            };
			
			// train the model
			var model =  teacher.Learn(inputs, outputs);
			
			// set array to be predicted
			double[] inputs2 = x[0];

            // compute the machine's answer for the array to be classified
            int answers = model.Decide(inputs2);
			
			// enter long if predicted value is 1
			if (answers == 1) 
			{
				EnterLong();	
			}
			
			// enter short if predicted value is 0
			if (answers == 0) 
			{
				EnterShort();
			}
			
			
			//Calculate the confusion matrix
			ConfusionMatrix cm = ConfusionMatrix.Estimate(model, inputs, outputs);
			
			false_neg = cm.FalseNegatives;
			false_pos = cm.FalsePositives;
			
			// Print false positive and false negative 
			Print(false_neg+ ",    " + false_pos);
        
        }

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade)
				return;
			
			if (Close[0]> Open[0])
			{
				myDir[0] = (double) 1;	
			}
			
			if (Close[0]<Open[0])
			{
				myDir[0] = (double) 0;	
			}
			
			// populate Label[] array
			for(int i=0; i<100; i++)
			{
				Label[i] = (int) myDir[i];
			}
			
			// populate x[] array
			for ( int i = 0; i < 5; i++)
			{
				x[0][i] = myDir[i];
			}
				
			
			for(int i=0; i<100; i++)
			{
				for (int j = 0; j<5; j++)
				{
					z[i][j] = myDir[i+j+1];
				}
			}
			
			// attempt to run the classifier
			try
			{
				Classifier();
			}
			catch (Exception e)
			{
				Print("error :(");
			}
		}
	}
}
