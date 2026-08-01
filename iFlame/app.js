/**
 * iFlame - ambient three-channel mixer
 *
 * Loop approach: stems are pre-processed with ffmpeg acrossfade self-joins
 * so start/end energy matches. Playback uses HTMLAudioElement.loop = true
 * routed through MediaElementAudioSourceNode into per-channel GainNodes.
 * Single buffer-style loop (no runtime staggered crossfade).
 *
 * Media elements (not fetch + decodeAudioData) so file:// open works in
 * browsers that block local XHR/fetch.
 */

(function () {
  "use strict";

  var IDLE_MS = 4000;
  var COOKIE_NAME = "iflame";
  var COOKIE_DAYS = 365;

  var DEFAULT_STATE = {
    fire: 0.55,
    rain: 0.35,
    noise: 0.2,
    muted: false,
  };

  var STEMS = {
    fire: [
      { src: "assets/fire.ogg", type: "audio/ogg; codecs=opus" },
      { src: "assets/fire.mp3", type: "audio/mpeg" },
    ],
    rain: [
      { src: "assets/rain.ogg", type: "audio/ogg; codecs=opus" },
      { src: "assets/rain.mp3", type: "audio/mpeg" },
    ],
    noise: [
      { src: "assets/noise.ogg", type: "audio/ogg; codecs=opus" },
      { src: "assets/noise.mp3", type: "audio/mpeg" },
    ],
  };

  /* ---------- Persistence (swappable) ---------- */

  function loadState() {
    var raw = readCookie(COOKIE_NAME);
    if (!raw) {
      return Object.assign({}, DEFAULT_STATE);
    }
    try {
      var parsed = JSON.parse(raw);
      return {
        fire: clamp01(num(parsed.fire, DEFAULT_STATE.fire)),
        rain: clamp01(num(parsed.rain, DEFAULT_STATE.rain)),
        noise: clamp01(num(parsed.noise, DEFAULT_STATE.noise)),
        muted: Boolean(parsed.muted),
      };
    } catch (err) {
      return Object.assign({}, DEFAULT_STATE);
    }
  }

  function saveState(state) {
    var payload = JSON.stringify({
      fire: clamp01(state.fire),
      rain: clamp01(state.rain),
      noise: clamp01(state.noise),
      muted: Boolean(state.muted),
    });
    writeCookie(COOKIE_NAME, payload, COOKIE_DAYS);
  }

  function readCookie(name) {
    var parts = ("; " + document.cookie).split("; " + name + "=");
    if (parts.length < 2) return null;
    var value = parts.pop().split(";").shift();
    try {
      return decodeURIComponent(value || "");
    } catch (err) {
      return null;
    }
  }

  function writeCookie(name, value, days) {
    var maxAge = Math.floor(days * 24 * 60 * 60);
    document.cookie =
      name +
      "=" +
      encodeURIComponent(value) +
      "; path=/; max-age=" +
      maxAge +
      "; SameSite=Lax";
  }

  /* ---------- Audio engine ---------- */

  var audio = {
    ctx: null,
    master: null,
    gains: { fire: null, rain: null, noise: null },
    elements: { fire: null, rain: null, noise: null },
    sources: { fire: null, rain: null, noise: null },
    started: false,
  };

  function createStemElement(key) {
    var el = document.createElement("audio");
    el.preload = "auto";
    el.loop = true;
    el.setAttribute("playsinline", "");
    el.setAttribute("aria-hidden", "true");
    el.style.display = "none";
    STEMS[key].forEach(function (stem) {
      var source = document.createElement("source");
      source.src = stem.src;
      source.type = stem.type;
      el.appendChild(source);
    });
    document.body.appendChild(el);
    el.load();
    return el;
  }

  function ensureGraph() {
    if (audio.ctx) return audio.ctx;

    var Ctx = window.AudioContext || window.webkitAudioContext;
    audio.ctx = new Ctx();
    audio.master = audio.ctx.createGain();
    audio.master.gain.value = 1;
    audio.master.connect(audio.ctx.destination);

    ["fire", "rain", "noise"].forEach(function (key) {
      var el = createStemElement(key);
      var gain = audio.ctx.createGain();
      gain.gain.value = 0;
      gain.connect(audio.master);

      var src = audio.ctx.createMediaElementSource(el);
      src.connect(gain);

      audio.elements[key] = el;
      audio.gains[key] = gain;
      audio.sources[key] = src;
    });

    return audio.ctx;
  }

  function applyGains(state) {
    if (!audio.ctx) return;
    var now = audio.ctx.currentTime;
    var muteFactor = state.muted ? 0 : 1;

    audio.master.gain.cancelScheduledValues(now);
    audio.master.gain.setTargetAtTime(muteFactor, now, 0.015);

    ["fire", "rain", "noise"].forEach(function (key) {
      var node = audio.gains[key];
      if (!node) return;
      node.gain.cancelScheduledValues(now);
      node.gain.setTargetAtTime(clamp01(state[key]), now, 0.015);
    });
  }

  function playAll() {
    var plays = ["fire", "rain", "noise"].map(function (key) {
      var el = audio.elements[key];
      var p = el.play();
      if (p && typeof p.then === "function") {
        return p.catch(function (err) {
          /* AbortError on rapid re-entry is benign */
          if (err && err.name === "AbortError") return;
          throw err;
        });
      }
      return Promise.resolve();
    });
    return Promise.all(plays);
  }

  function lightFire(state) {
    ensureGraph();
    return audio.ctx.resume().then(function () {
      applyGains(state);
      return playAll();
    }).then(function () {
      audio.started = true;
    });
  }

  /* ---------- UI ---------- */

  var state = loadState();
  var tray = document.getElementById("tray");
  var igniteBtn = document.getElementById("ignite");
  var channels = document.getElementById("channels");
  var muteBtn = document.getElementById("mute");
  var sliders = {
    fire: document.getElementById("gain-fire"),
    rain: document.getElementById("gain-rain"),
    noise: document.getElementById("gain-noise"),
  };

  var idleTimer = null;

  function syncUiFromState() {
    sliders.fire.value = String(Math.round(state.fire * 100));
    sliders.rain.value = String(Math.round(state.rain * 100));
    sliders.noise.value = String(Math.round(state.noise * 100));
    muteBtn.setAttribute("aria-pressed", state.muted ? "true" : "false");
    muteBtn.textContent = state.muted ? "Unmute" : "Mute";

    if (audio.started) {
      igniteBtn.hidden = true;
      channels.hidden = false;
    } else {
      igniteBtn.hidden = false;
      channels.hidden = true;
      igniteBtn.setAttribute("aria-pressed", "false");
    }
  }

  function onSliderInput(key, event) {
    var value = Number(event.target.value) / 100;
    if (!isFinite(value)) return;
    state[key] = clamp01(value);
    if (audio.started) applyGains(state);
    saveState(state);
  }

  function showTray() {
    tray.classList.remove("is-idle");
    clearTimeout(idleTimer);
    idleTimer = setTimeout(function () {
      tray.classList.add("is-idle");
    }, IDLE_MS);
  }

  Object.keys(sliders).forEach(function (key) {
    sliders[key].addEventListener("input", function (e) {
      onSliderInput(key, e);
    });
  });

  muteBtn.addEventListener("click", function () {
    state.muted = !state.muted;
    if (audio.started) applyGains(state);
    saveState(state);
    syncUiFromState();
    showTray();
  });

  igniteBtn.addEventListener("click", function () {
    igniteBtn.disabled = true;
    igniteBtn.textContent = "Lighting…";
    lightFire(state)
      .then(function () {
        igniteBtn.hidden = true;
        channels.hidden = false;
        igniteBtn.setAttribute("aria-pressed", "true");
        showTray();
      })
      .catch(function (err) {
        console.error("iFlame: failed to start audio", err);
        igniteBtn.disabled = false;
        igniteBtn.textContent = "Light the fire";
      });
  });

  ["mousemove", "keydown", "touchstart"].forEach(function (evt) {
    window.addEventListener(evt, showTray, { passive: true });
  });

  syncUiFromState();
  showTray();

  /* ---------- helpers ---------- */

  function clamp01(n) {
    if (n < 0) return 0;
    if (n > 1) return 1;
    return n;
  }

  function num(v, fallback) {
    var n = Number(v);
    return isFinite(n) ? n : fallback;
  }
})();
