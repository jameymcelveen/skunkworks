/* <press-button> — same key component as the hub demo. */
class PressButton extends HTMLElement {
  static get observedAttributes() { return ['label', 'sub', 'state']; }
  connectedCallback() { this.render(); }
  attributeChangedCallback() { if (this.shadowRoot) this.render(); }

  render() {
    const label = this.getAttribute('label') || '';
    const sub = this.getAttribute('sub') || '';
    const state = this.getAttribute('state') || 'ghost';
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
        :host([state="ghost"]) .key {
          --edge: #C9C2B6; opacity: .4; filter: grayscale(0.4);
        }
        :host([state="active"]) .key {
          box-shadow: 0 0 0 4px rgba(185,71,0,0.14);
          animation: pulse 1.6s ease-in-out infinite;
        }
        :host([state="done"]) .key { --edge: #546223; }
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

/* ---- Data ownership walkthrough ----
   Edit STEPS to change click order or wording. */
const STEPS = [
  {
    caption: `<b>One database.</b> ChristMed is a single Postgres. Every church, every trip, every patient lives here at rest.`,
    on: ['christmed', 'db']
  },
  {
    caption: `<b>Tenants are churches.</b> Cornerstone is on this trip. First Baptist is another tenant in the same DB — walls stay up.`,
    on: ['cornerstone', 'tenant', 'peer', 'firstbaptist'], press: 'christmed'
  },
  {
    caption: `<b>Clinics are mission trips.</b> Belize and Honduras are both Cornerstone clinics. Same church, separate field weeks.`,
    on: ['belize', 'honduras', 'clinic-belize', 'clinic-honduras'], press: 'cornerstone'
  },
  {
    caption: `<b>Shared @ church.</b> Formulary, mission workers, and treatments live above the clinics. Both trips read and can extend the same catalogs — merge is additive, never a clobber.`,
    on: ['shared', 'chip-formulary', 'chip-workers', 'chip-treatments'], press: 'cornerstone'
  },
  {
    caption: `<b>Patients stay in one clinic.</b> Belize encounters and Rx do not bleed into Honduras. That is the hard scope line.`,
    on: ['chip-bz-patients', 'chip-bz-encounters', 'chip-bz-rx',
         'chip-hn-patients', 'chip-hn-encounters', 'chip-hn-rx'], press: 'belize'
  },
  {
    caption: `<b>Checkout for Belize.</b> The hub takes the Belize clinic slice plus a snapshot of the shared band. Honduras stays home.`,
    on: ['a-checkout', 'hub', 'payload', 'pay-clinic', 'pay-shared', 'hold-honduras'],
    press: 'belize',
    hold: ['clinic-honduras']
  },
  {
    caption: `<b>Nightly push.</b> At the hotel, one-way bulk from hub to cloud over the legal link. Shared additions travel with it; still one writer, one direction.`,
    on: ['a-nightly'], press: 'hub'
  },
  {
    caption: `<b>Check-in.</b> Belize clinical records reconcile home (checkout wins). Shared rows merge additively into the church catalog. The Belize hold releases; Honduras never moved.`,
    on: ['a-checkin', 'christmed', 'shared', 'chip-formulary', 'chip-workers', 'chip-treatments',
         'chip-bz-patients', 'chip-bz-encounters', 'chip-bz-rx'],
    press: 'hub'
  }
];

let step = -1;

function allLitTargets() {
  return document.querySelectorAll(
    'press-button, .arrow, .dchip, .data-shell, .data-tenant, .data-shared, .data-clinic, .hub-payload'
  );
}

function apply(idx) {
  const s = STEPS[idx];
  document.getElementById('caption').innerHTML = s.caption;

  if (s.press) {
    const el = document.getElementById(s.press);
    if (el && el.press) el.press();
  }

  s.on.forEach(id => {
    if (id === 'hold-honduras') return;
    const el = document.getElementById(id);
    if (!el) return;
    if (el.tagName === 'PRESS-BUTTON') el.setAttribute('state', 'done');
    else el.classList.add('lit');
  });

  if (s.hold) {
    s.hold.forEach(id => {
      const el = document.getElementById(id);
      if (el) el.classList.add('held');
    });
  }

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
  allLitTargets().forEach(el => {
    el.classList.remove('lit', 'held');
  });
  document.getElementById('caption').innerHTML =
    `Click <b>Advance</b> or press the space bar to walk ownership from the database down to a Belize checkout and back.`;
  document.getElementById('count').textContent = `0 / ${STEPS.length}`;
  document.getElementById('prev').disabled = true;
  document.getElementById('next').disabled = false;
}

function next() { if (step < STEPS.length - 1) { step++; apply(step); } }
function prev() {
  if (step <= 0) { reset(); return; }
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
