mergeInto(LibraryManager.library, {
  StartMicAnalysis: function () {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
      console.error("Mic not supported");
      return;
    }

    navigator.mediaDevices.getUserMedia({ audio: true }).then((stream) => {
      const ctx = new (window.AudioContext || window.webkitAudioContext)();
      const mic = ctx.createMediaStreamSource(stream);
      const analyser = ctx.createAnalyser();
      analyser.fftSize = 2048;

      const buffer = new Uint8Array(analyser.frequencyBinCount);
      mic.connect(analyser);

      const nyquist = ctx.sampleRate / 2;
      const binSize = nyquist / analyser.frequencyBinCount;

      function update() {
        analyser.getByteFrequencyData(buffer);

        // Loudness = average of spectrum
        let sum = 0;
        for (let i = 0; i < buffer.length; i++) {
          sum += buffer[i];
        }
        const loudness = sum / buffer.length;

        // Frequency = dominant bin
        let maxVal = -1, maxIndex = -1;
        for (let i = 0; i < buffer.length; i++) {
          if (buffer[i] > maxVal) {
            maxVal = buffer[i];
            maxIndex = i;
          }
        }
        const frequency = (maxIndex * binSize).toFixed(1);

        if (typeof SendMessage === "function") {
          SendMessage("MicAnalysisReceiver", "OnMicLoudness", loudness.toString());
          SendMessage("MicAnalysisReceiver", "OnMicFrequency", frequency.toString());
        }

        requestAnimationFrame(update);
      }

      update();
    }).catch(err => console.error("Mic error:", err));
  }
});
