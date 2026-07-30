/* <press-button> — a round-rect key with a heavy bottom border that
   depresses when clicked. Reusable across the diagram. */
class PressButton extends HTMLElement {
  static get observedAttributes() { return ['label', 'sub', 'state']; }
  connectedCallback() { this.render(); }
  attributeChangedCallback() { if (this.shadowRoot) this.render(); }

  render() {
    const label = this.getAttribute('label') || '';
    const sub = this.getAttribute('sub') || '';
    const state = this.getAttribute('state') || 'ghost'; // ghost | active | done
    if (!this.shadowRoot) this.attachShadow({ mode: 'open' });
    this.shadowRoot.innerHTML = `
      <style>
        :host { display: inline-block; }
        .key {
          --fill: #ffffff; --edge: #B94700; --txt: #2E2A26;
          position: relative; min-width: 132px; padding: 14px 18px 15px;
          background: var(--fill); color: var(--txt);
          border: 1.5px solid var(--edge);
          border-bottom-width: 6px; border-radius: 12px;
          font-family: 'Bricolage Grotesque','Public Sans',system-ui,sans-serif;
          text-align: center; cursor: default; user-select: none;
          transition: transform .12s ease, border-bottom-width .12s ease,
                      opacity .4s ease, filter .3s ease;
        }
        .name { font-weight: 700; font-size: 15px; letter-spacing: -0.01em; }
        .sub {
          font-family: 'DejaVu Sans Mono', ui-monospace, monospace;
          font-size: 10px; letter-spacing: .04em; color: #8a8378; margin-top: 3px;
        }
        /* not yet reached */
        :host([state="ghost"]) .key {
          --edge: #C9C2B6; opacity: .4; filter: grayscale(0.4);
        }
        /* current focus */
        :host([state="active"]) .key {
          box-shadow: 0 0 0 4px rgba(185,71,0,0.14);
          animation: pulse 1.6s ease-in-out infinite;
        }
        /* reached and settled */
        :host([state="done"]) .key { --edge: #546223; }
        /* the press */
        :host(.pressed) .key {
          transform: translateY(4px); border-bottom-width: 2px;
        }
        @keyframes pulse {
          0%,100% { box-shadow: 0 0 0 4px rgba(185,71,0,0.14); }
          50%     { box-shadow: 0 0 0 7px rgba(185,71,0,0.05); }
        }
        @media (prefers-reduced-motion: reduce) {
          .key { animation: none !important; }
        }
      </style>
      <div class="key">
        <div class="name">${label}</div>
        ${sub ? `<div class="sub">${sub}</div>` : ''}
      </div>`;
  }

  press() {
    this.classList.add('pressed');
    setTimeout(() => this.classList.remove('pressed'), 150);
  }
}
customElements.define('press-button', PressButton);

/* ---- The story sequence ----
   Each step: caption text + which nodes/arrows/devices become active.
   Edit this array to change click order or wording. */
const STEPS = [
  {
    caption: `<b>ChristMed</b> is the home system, the master record back in the States. Everything starts here.`,
    on: ['christmed']
  },
  {
    caption: `Reference tables — the formulary and diagnosis dictionaries — are prepared for a trip. <b>Shared tables merge additively:</b> one trip never clobbers another's additions.`,
    on: ['a-christmed-cornerstone'], press: 'christmed'
  },
  {
    caption: `<b>Cornerstone Church</b> is prepped for its mission. A church is a tenant; each runs its own trips.`,
    on: ['cornerstone']
  },
  {
    caption: `<b>First Baptist</b> too. Multiple churches, multiple trips, one home system feeding them all.`,
    on: ['firstbaptist', 'a-cornerstone-firstbaptist'], press: 'cornerstone'
  },
  {
    caption: `<b>Checkout.</b> For the duration of a trip, the Belize hub becomes the master for clinical records. Checked-out data wins on return.`,
    on: ['a-checkout', 'hub'], press: 'firstbaptist'
  },
  {
    caption: `The <b>hub travels to the clinic site</b> in Belize. Belize law routes all internet through official channels, so there is no legitimate live cloud link during clinic hours.`,
    on: ['a-hub-location'], press: 'hub'
  },
  {
    caption: `Field <b>tablets connect to the hub over a local network</b> — a travel router, no internet. The nurse's vitals are visible to the doctor five minutes later, on a different device. That cross-device visibility is the real reason for the hub.`,
    on: ['a-hub-location', 'dev1', 'dev2', 'dev3'], press: 'hub'
  },
  {
    caption: `<b>Nightly, at the hotel</b>, the hub does a one-way bulk push to the cloud over the legal connection. One writer, one direction, once a day, at rest. Not the old sync monster.`,
    on: ['a-hub-cloud'], press: 'hub'
  },
  {
    caption: `<b>Trip ends: check-in.</b> The Belize hold releases, clinical records reconcile home, dictionary additions merge back. The system is whole again. <span style="color:#546223">Same shape as a cruise ship: a local hub behind a controlled boundary, reconciling on a schedule.</span>`,
    on: ['christmed', 'a-release'], press: 'hub'
  }
];

const nodes = () => ({
  christmed:    document.getElementById('christmed'),
  cornerstone:  document.getElementById('cornerstone'),
  firstbaptist: document.getElementById('firstbaptist'),
  hub:          document.getElementById('hub'),
});

let step = -1;

function apply(idx) {
  const s = STEPS[idx];
  document.getElementById('caption').innerHTML = s.caption;

  // press the outgoing button if named
  if (s.press) {
    const el = document.getElementById(s.press);
    if (el && el.press) el.press();
  }

  // activate everything named in this step
  s.on.forEach(id => {
    const el = document.getElementById(id);
    if (!el) return;
    if (el.tagName === 'PRESS-BUTTON') el.setAttribute('state', 'done');
    else el.classList.add('lit');
  });

  // set the newest press-button as active-focus, downgrade prior actives to done
  document.querySelectorAll('press-button').forEach(b => {
    if (b.getAttribute('state') === 'active') b.setAttribute('state', 'done');
  });
  const lastBtn = s.on.map(id => document.getElementById(id))
                      .filter(el => el && el.tagName === 'PRESS-BUTTON').pop();
  if (lastBtn) lastBtn.setAttribute('state', 'active');

  document.getElementById('count').textContent = `${idx + 1} / ${STEPS.length}`;
  document.getElementById('prev').disabled = idx <= 0;
  document.getElementById('next').disabled = idx >= STEPS.length - 1;
}

function reset() {
  step = -1;
  document.querySelectorAll('press-button').forEach(b => b.setAttribute('state', 'ghost'));
  document.querySelectorAll('.arrow').forEach(a => a.classList.remove('lit'));
  document.querySelectorAll('.device').forEach(d => d.classList.remove('lit'));
  document.getElementById('caption').innerHTML =
    `Click <b>Advance</b> or press the space bar to walk the data from the home system to the field and back.`;
  document.getElementById('count').textContent = `0 / ${STEPS.length}`;
  document.getElementById('prev').disabled = true;
  document.getElementById('next').disabled = false;
}

function next() { if (step < STEPS.length - 1) { step++; apply(step); } }
function prev() {
  if (step <= 0) { reset(); return; }
  // rebuild state up to step-1 (simplest correct approach)
  const target = step - 1;
  reset();
  for (let i = 0; i <= target; i++) { step = i; apply(i); }
}

window.addEventListener('DOMContentLoaded', () => {
  document.getElementById('next').addEventListener('click', next);
  document.getElementById('prev').addEventListener('click', prev);
  document.getElementById('reset').addEventListener('click', reset);
  document.addEventListener('keydown', e => {
    if (e.code === 'Space' || e.code === 'ArrowRight') { e.preventDefault(); next(); }
    if (e.code === 'ArrowLeft') { e.preventDefault(); prev(); }
  });
  reset();
});
