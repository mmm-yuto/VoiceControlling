mergeInto(LibraryManager.library, {
    // グローバル変数
    webglMicrophoneContext: null,
    webglMicrophoneSource: null,
    webglMicrophoneProcessor: null,
    webglMicrophoneStream: null,
    webglMicrophoneSampleRate: 44100,
    webglMicrophoneBufferSize: 1024,
    webglMicrophoneSamples: null,
    webglMicrophoneIsRecording: false,
    webglMicrophoneInitialized: false,

    // マイク初期化
    InitializeMicrophone: function (sampleRate, bufferSize) {
        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            console.error("WebGL Microphone: getUserMedia is not supported");
            return false;
        }

        webglMicrophoneSampleRate = sampleRate;
        webglMicrophoneBufferSize = bufferSize;
        webglMicrophoneSamples = new Float32Array(bufferSize);
        webglMicrophoneInitialized = true;

        console.log("WebGL Microphone: Initialized with sampleRate=" + sampleRate + ", bufferSize=" + bufferSize);
        return true;
    },

    // 録音開始
    StartRecording: function () {
        if (!webglMicrophoneInitialized) {
            console.error("WebGL Microphone: Not initialized. Call InitializeMicrophone first.");
            return false;
        }

        if (webglMicrophoneIsRecording) {
            console.log("WebGL Microphone: Already recording");
            return true;
        }

        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                try {
                    webglMicrophoneStream = stream;
                    
                    // UnityのAudioContextをresume（Unityroom環境でのSuspended状態を解除）
                    try {
                        if (typeof unityInstance !== 'undefined' && unityInstance && unityInstance.Module && unityInstance.Module.audioContext) {
                            var unityAudioContext = unityInstance.Module.audioContext;
                            if (unityAudioContext.state === 'suspended') {
                                unityAudioContext.resume().then(function() {
                                    console.log("WebGL Microphone: Unity AudioContext resumed");
                                }).catch(function(e) {
                                    console.warn("WebGL Microphone: Failed to resume Unity AudioContext: " + e);
                                });
                            }
                        }
                    } catch (e) {
                        console.warn("WebGL Microphone: Could not access Unity AudioContext: " + e);
                    }
                    
                    // AudioContextの作成
                    var AudioContext = window.AudioContext || window.webkitAudioContext;
                    webglMicrophoneContext = new AudioContext({ sampleRate: webglMicrophoneSampleRate });
                    
                    // AudioContextがSuspended状態の場合はresume
                    if (webglMicrophoneContext.state === 'suspended') {
                        webglMicrophoneContext.resume().then(function() {
                            console.log("WebGL Microphone: Microphone AudioContext resumed");
                        }).catch(function(e) {
                            console.warn("WebGL Microphone: Failed to resume Microphone AudioContext: " + e);
                        });
                    }
                    
                    // マイク入力の取得
                    webglMicrophoneSource = webglMicrophoneContext.createMediaStreamSource(stream);
                    
                    // ScriptProcessorNodeの作成（AudioWorkletのフォールバック）
                    webglMicrophoneProcessor = webglMicrophoneContext.createScriptProcessor(webglMicrophoneBufferSize, 1, 1);
                    
                    webglMicrophoneProcessor.onaudioprocess = function (e) {
                        if (!webglMicrophoneIsRecording) return;
                        
                        var inputData = e.inputBuffer.getChannelData(0);
                        
                        // サンプルデータをコピー
                        for (var i = 0; i < Math.min(inputData.length, webglMicrophoneBufferSize); i++) {
                            webglMicrophoneSamples[i] = inputData[i];
                        }
                    };
                    
                    // 接続
                    webglMicrophoneSource.connect(webglMicrophoneProcessor);
                    webglMicrophoneProcessor.connect(webglMicrophoneContext.destination);
                    
                    webglMicrophoneIsRecording = true;
                    console.log("WebGL Microphone: Recording started successfully");
                } catch (error) {
                    console.error("WebGL Microphone: Error setting up audio: " + error);
                    if (webglMicrophoneStream) {
                        webglMicrophoneStream.getTracks().forEach(function (track) {
                            track.stop();
                        });
                        webglMicrophoneStream = null;
                    }
                    webglMicrophoneIsRecording = false;
                }
            })
            .catch(function (error) {
                console.error("WebGL Microphone: Error accessing microphone: " + error);
                console.error("WebGL Microphone: Error name: " + error.name + ", message: " + error.message);
                if (error.name === 'NotAllowedError') {
                    console.error("WebGL Microphone: Microphone access denied by user or browser policy");
                } else if (error.name === 'NotFoundError') {
                    console.error("WebGL Microphone: No microphone found");
                } else if (error.name === 'NotReadableError') {
                    console.error("WebGL Microphone: Microphone is already in use");
                } else if (error.name === 'OverconstrainedError') {
                    console.error("WebGL Microphone: Microphone constraints cannot be satisfied");
                }
                webglMicrophoneIsRecording = false;
                return false;
            });

        return true;
    },

    // 録音停止
    StopRecording: function () {
        if (!webglMicrophoneIsRecording) {
            return true;
        }

        webglMicrophoneIsRecording = false;

        if (webglMicrophoneProcessor) {
            try {
                webglMicrophoneProcessor.disconnect();
            } catch (e) {
                console.warn("WebGL Microphone: Error disconnecting processor: " + e);
            }
            webglMicrophoneProcessor = null;
        }

        if (webglMicrophoneSource) {
            try {
                webglMicrophoneSource.disconnect();
            } catch (e) {
                console.warn("WebGL Microphone: Error disconnecting source: " + e);
            }
            webglMicrophoneSource = null;
        }

        if (webglMicrophoneContext) {
            webglMicrophoneContext.close().catch(function (e) {
                console.warn("WebGL Microphone: Error closing context: " + e);
            });
            webglMicrophoneContext = null;
        }

        if (webglMicrophoneStream) {
            webglMicrophoneStream.getTracks().forEach(function (track) {
                track.stop();
            });
            webglMicrophoneStream = null;
        }

        console.log("WebGL Microphone: Recording stopped");
        return true;
    },

    // 録音状態の確認
    IsRecording: function () {
        return webglMicrophoneIsRecording ? 1 : 0;
    },

    // 音声サンプルデータの取得
    GetAudioSamples: function (samplesPtr, bufferSize) {
        if (!webglMicrophoneIsRecording || !webglMicrophoneSamples) {
            return false;
        }

        if (!samplesPtr) {
            return false;
        }

        var samples = HEAPF32.subarray(samplesPtr / 4, samplesPtr / 4 + bufferSize);
        var copyLength = Math.min(webglMicrophoneSamples.length, bufferSize);
        
        for (var i = 0; i < copyLength; i++) {
            samples[i] = webglMicrophoneSamples[i];
        }
        
        // 残りの部分を0で埋める
        for (var i = copyLength; i < bufferSize; i++) {
            samples[i] = 0.0;
        }

        return true;
    },

    // サンプルレートの取得
    GetSampleRate: function () {
        return webglMicrophoneSampleRate;
    },

    // バッファサイズの取得
    GetBufferSize: function () {
        return webglMicrophoneBufferSize;
    }
});
