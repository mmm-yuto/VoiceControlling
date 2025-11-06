using UnityEngine;
using UnityEngine.UI;

public class VoiceScatterPlot : MonoBehaviour
{
	[Header("Plot References")]
	public RectTransform plotArea; // プロット範囲（x=Volume, y=Pitch）
	public RectTransform marker;   // 現在位置を示すマーカー
	public Color markerColor = Color.red;

	[Header("Ranges")] 
	public float maxVolume = 1f;     // x 範囲: 0..maxVolume
	public float minPitch = 80f;     // y 最小
	public float maxPitch = 1000f;   // y 最大

	[Header("Smoothing")] 
	[Range(0f, 1f)] public float smoothing = 0.2f; // 位置スムージング係数
	[Tooltip("ImprovedPitchAnalyzerが無い場合に使う無音判定の音量閾値")]
	public float silenceVolumeThreshold = 0.01f;
	[Tooltip("この時間ピッチ更新が無ければ無音とみなす（秒）")]
	public float pitchStaleTimeout = 0.25f;

	private VolumeAnalyzer volumeAnalyzer;
	private ImprovedPitchAnalyzer improvedPitchAnalyzer;
	private PitchAnalyzer pitchAnalyzer;
	private FixedPitchAnalyzer fixedPitchAnalyzer;
	private VoiceDisplay voiceDisplay;

	private float latestVolume = 0f;
	private float latestPitch = 0f;
	private float lastPitchUpdateTime = -999f;
	private float zeroVolume = 0f; // キャリブ平均で決定（a）
	private float zeroPitch = 80f; // キャリブ平均で決定（a）
	private Vector2 smoothedPos;

	void Awake()
	{
		// 参照が無い場合は自動生成
		EnsurePlotUI();
	}

	void Start()
	{
		// 分析コンポーネント取得
		volumeAnalyzer = FindObjectOfType<VolumeAnalyzer>();
		improvedPitchAnalyzer = FindObjectOfType<ImprovedPitchAnalyzer>();
		pitchAnalyzer = FindObjectOfType<PitchAnalyzer>();
		fixedPitchAnalyzer = FindObjectOfType<FixedPitchAnalyzer>();

		// 範囲同期（存在すれば優先）
		voiceDisplay = FindObjectOfType<VoiceDisplay>();
		if (voiceDisplay != null)
		{
			SyncRangesFromUI();
		}
		else if (improvedPitchAnalyzer != null)
		{
			minPitch = improvedPitchAnalyzer.minFrequency;
			maxPitch = improvedPitchAnalyzer.maxFrequency;
		}

		// 原点（0点）をキャリブ平均で初期化（a）
		zeroVolume = Mathf.Max(0f, VoiceCalibrator.LastAverageVolume);
		zeroPitch = VoiceCalibrator.LastAveragePitch > 0f ? VoiceCalibrator.LastAveragePitch : minPitch;

		// キャリブ完了通知が来たら原点を更新
		VoiceCalibrator.OnCalibrationAveragesUpdated += OnCalibrationAveragesUpdated;

		// イベント購読
		if (volumeAnalyzer != null)
		{
			volumeAnalyzer.OnVolumeDetected += OnVolume;
		}

		if (fixedPitchAnalyzer != null)
		{
			fixedPitchAnalyzer.OnPitchDetected += OnPitch;
		}
		else if (improvedPitchAnalyzer != null)
		{
			improvedPitchAnalyzer.OnPitchDetected += OnPitch;
		}
		else if (pitchAnalyzer != null)
		{
			pitchAnalyzer.OnPitchDetected += OnPitch;
		}

		// 初期位置は中心に配置
		smoothedPos = Vector2.zero;
		CenterMarker();
	}

	void OnDestroy()
	{
		if (volumeAnalyzer != null)
			volumeAnalyzer.OnVolumeDetected -= OnVolume;
		if (fixedPitchAnalyzer != null)
			fixedPitchAnalyzer.OnPitchDetected -= OnPitch;
		if (improvedPitchAnalyzer != null)
			improvedPitchAnalyzer.OnPitchDetected -= OnPitch;
		if (pitchAnalyzer != null)
			pitchAnalyzer.OnPitchDetected -= OnPitch;
		VoiceCalibrator.OnCalibrationAveragesUpdated -= OnCalibrationAveragesUpdated;
	}

	void OnVolume(float v)
	{
		latestVolume = v;
	}

	void OnPitch(float p)
	{
		latestPitch = p;
		lastPitchUpdateTime = Time.time;
	}

	void Update()
	{
		UpdateMarkerPosition(latestVolume, latestPitch);
	}

	void UpdateMarkerPosition(float volume, float pitch)
	{
		if (plotArea == null || marker == null) return;

		// 無音判定: ImprovedPitchAnalyzerの閾値があればそれを優先、無ければローカル閾値を使用
		float threshold = improvedPitchAnalyzer != null ? improvedPitchAnalyzer.volumeThreshold : silenceVolumeThreshold;
		bool pitchStale = (Time.time - lastPitchUpdateTime) > pitchStaleTimeout;
		bool isSilent = (volume < threshold) || (pitch <= 0f) || pitchStale;
		if (isSilent)
		{
			CenterMarker();
			return;
		}

		// b) 原点を中心に負側も表示（左右/上下で非対称レンジ対応）
		float leftExtent = Mathf.Max(0.0001f, zeroVolume - 0f);
		float rightExtent = Mathf.Max(0.0001f, maxVolume - zeroVolume);
		float downExtent = Mathf.Max(0.0001f, zeroPitch - minPitch);
		float upExtent = Mathf.Max(0.0001f, maxPitch - zeroPitch);

		float x01;
		if (volume >= zeroVolume)
		{
			float frac = (volume - zeroVolume) / rightExtent;
			x01 = 0.5f + 0.5f * Mathf.Clamp01(frac);
		}
		else
		{
			float frac = (zeroVolume - volume) / leftExtent;
			x01 = 0.5f - 0.5f * Mathf.Clamp01(frac);
		}

		float y01;
		if (pitch >= zeroPitch)
		{
			float frac = (pitch - zeroPitch) / upExtent;
			y01 = 0.5f + 0.5f * Mathf.Clamp01(frac);
		}
		else
		{
			float frac = (zeroPitch - pitch) / downExtent;
			y01 = 0.5f - 0.5f * Mathf.Clamp01(frac);
		}

		Vector2 size = plotArea.rect.size;
		// RectTransformのpivotが中心(0.5,0.5)の場合、左下は(-size/2)、右上は(+size/2)
		Vector2 bottomLeft = new Vector2(-0.5f * size.x, -0.5f * size.y);
		Vector2 target = bottomLeft + new Vector2(x01 * size.x, y01 * size.y);
		smoothedPos = Vector2.Lerp(smoothedPos, target, Mathf.Clamp01(smoothing));
		marker.anchoredPosition = smoothedPos;
	}

	void CenterMarker()
	{
		if (plotArea == null || marker == null) return;
		// 中心はpivot基準で(0,0)
		Vector2 center = Vector2.zero;
		smoothedPos = Vector2.Lerp(smoothedPos, center, Mathf.Clamp01(smoothing));
		marker.anchoredPosition = smoothedPos;
	}

	void OnCalibrationAveragesUpdated(float avgVol, float avgPitch)
	{
		zeroVolume = Mathf.Max(0f, avgVol);
		zeroPitch = avgPitch > 0f ? avgPitch : zeroPitch;
		// バー側の範囲更新を反映
		SyncRangesFromUI();
	}

	void SyncRangesFromUI()
	{
		if (voiceDisplay == null) return;
		maxVolume = Mathf.Max(0.0001f, voiceDisplay.maxVolume);
		minPitch = voiceDisplay.minPitch;
		maxPitch = voiceDisplay.maxPitch;
	}

	void EnsurePlotUI()
	{
		if (plotArea != null && marker != null) return;

		// Canvas を探す/作る
		Canvas canvas = FindObjectOfType<Canvas>();
		if (canvas == null)
		{
			GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			canvas = canvasGo.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		}

		// PlotArea を用意
		if (plotArea == null)
		{
			GameObject areaGo = new GameObject("VoicePlotArea", typeof(RectTransform), typeof(Image));
			areaGo.transform.SetParent(canvas.transform, false);
			plotArea = areaGo.GetComponent<RectTransform>();
			plotArea.anchorMin = new Vector2(0.7f, 0.05f);
			plotArea.anchorMax = new Vector2(0.98f, 0.45f);
			plotArea.offsetMin = Vector2.zero;
			plotArea.offsetMax = Vector2.zero;
			var bg = areaGo.GetComponent<Image>();
			bg.color = new Color(0f, 0f, 0f, 0.3f);
		}

		// Marker を用意
		if (marker == null)
		{
			GameObject markerGo = new GameObject("VoiceMarker", typeof(RectTransform), typeof(Image));
			markerGo.transform.SetParent(plotArea, false);
			marker = markerGo.GetComponent<RectTransform>();
			marker.sizeDelta = new Vector2(12f, 12f);
			var img = markerGo.GetComponent<Image>();
			img.color = markerColor;
		}
	}
}


