// Phản hồi khi quét (DESIGN.md "The Not-Only-Color Rule"): tiếng bíp + rung, không chỉ đổi màu.
// Điện thoại chặn âm thanh không do người dùng khởi phát: gọi unlockAudio() trong một lần bấm nút.

let audio: AudioContext | null = null

export function unlockAudio(): void {
  try {
    audio ??= new AudioContext()
    if (audio.state === 'suspended') void audio.resume()
  } catch {
    // Trình duyệt không có Web Audio: chỉ còn rung.
  }
}

// ok: bíp cao ngắn (thêm vào giỏ). warn: hai tiếng trầm (mã chưa có, lỗi).
export function scanFeedback(kind: 'ok' | 'warn'): void {
  navigator.vibrate?.(kind === 'ok' ? 60 : [80, 60, 80])
  if (!audio || audio.state !== 'running') return
  const tones = kind === 'ok' ? [{ freq: 1500, at: 0 }] : [{ freq: 440, at: 0 }, { freq: 440, at: 0.16 }]
  for (const tone of tones) {
    const osc = audio.createOscillator()
    const gain = audio.createGain()
    osc.frequency.value = tone.freq
    gain.gain.value = 0.15
    osc.connect(gain).connect(audio.destination)
    osc.start(audio.currentTime + tone.at)
    osc.stop(audio.currentTime + tone.at + 0.09)
  }
}
