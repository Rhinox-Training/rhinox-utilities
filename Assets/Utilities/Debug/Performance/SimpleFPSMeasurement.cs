using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSDisplay2 : MonoBehaviour
{
    //ENUMS & BITFLAGS
    public enum eVTelemetryMode
    {
        Minimal = 0,
        Normal = 1,
        Expanded = 2,
        Full = 3
    }

    private int mTimedFrameCount = 0;
	private float mTimedElapedTime = 0.0f;
    private float mTimedFrameRate = 0.0f;
    [SerializeField]private float mRefreshTime = 0.5f;
    [SerializeField]private eVTelemetryMode mDisplayMode = eVTelemetryMode.Minimal;
    private float mCurrentFrameRate = 1.0f;
    private float mTimedMinimum = 10000.0f;
    private float mTimedMaximum = 0.0f;

    [SerializeField]private Text mTextComponent = null;
    [SerializeField]private TextMesh mTextMeshComponent = null;
    private string mStringBuffer = "";

	public int TargetFrameRate = 1000;
	
	
	void Start ()
	{
        if (this.mTextComponent == null) { this.mTextComponent = GetComponent<Text>(); }
        if (this.mTextMeshComponent == null) { this.mTextMeshComponent = GetComponent<TextMesh>(); }
		
		//ENFORCE GFX SETTINGS
		Application.targetFrameRate = TargetFrameRate;
		QualitySettings.vSyncCount = 0; //30FPS
		//QualitySettings.antiAliasing = 0;
	}
	

	void Update()
	{

        //Note that we're measuring Update Game Cycles here, not actually spit out frames by the GPU
        mCurrentFrameRate = 1 / Time.deltaTime;
        if (mCurrentFrameRate > mTimedMaximum) { mTimedMaximum = mCurrentFrameRate; }
        if (mCurrentFrameRate < mTimedMinimum) { mTimedMinimum = mCurrentFrameRate; }

        if ( mTimedElapedTime < mRefreshTime ) //Update Data
		{
			mTimedElapedTime += Time.deltaTime;
			mTimedFrameCount++;
        }
		else //Calculate & Reset Data
		{
			//This code will break if you set your m_refreshTime to 0, which makes no sense.
			mTimedFrameRate = (float)mTimedFrameCount/mTimedElapedTime; //Average Sampled

            this.mStringBuffer = "";

            //Present
            if (mDisplayMode == eVTelemetryMode.Normal)
            {
                this.mStringBuffer = "AVG: " + this.mTimedFrameRate.ToString("F1") + " FPS" +
                "\n" + "MIN: " + this.mTimedMinimum.ToString("F1") + " FPS" +
                "\n" + "MAX: " + this.mTimedMaximum.ToString("F1") + " FPS" +
                "\n" + "CUR: " + this.mCurrentFrameRate.ToString("F1") + " FPS";
            }
            else { this.mStringBuffer = this.mTimedFrameRate.ToString("F1") + " FPS"; }
            
            if (this.mTextComponent != null) { this.mTextComponent.text = this.mStringBuffer; }
            if (this.mTextMeshComponent != null) { this.mTextMeshComponent.text = this.mStringBuffer; }
           
            //Reset Cycle
            mTimedFrameCount = 0;
            mTimedElapedTime = 0.0f;
            mTimedMinimum = 10000.0f;
            mTimedMaximum = 0.0f;
        }
	}
}



