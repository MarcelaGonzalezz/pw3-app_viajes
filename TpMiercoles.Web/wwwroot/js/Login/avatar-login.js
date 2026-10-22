/*
 * Avatar animado del login.
 * Adaptado de "Animated SVG Avatar v2" (dsenneff, CodePen) a GSAP 3.
 *
 * Requiere gsap y MorphSVGPlugin cargados antes de este archivo.
 *
 * Atributos que el script busca en tu formulario:
 *   [data-avatar-email]     input del email
 *   [data-avatar-password]  input de la contraseña
 *   [data-avatar-toggle]    checkbox "mostrar contraseña" (opcional)
 */
(() => {
    'use strict';

    const SVG_SIZE = 200; // lado del viewBox del SVG
    const MIRROR_PROPS = [
        'font-family', 'font-size', 'font-weight', 'font-style', 'letter-spacing',
        'text-transform', 'text-indent', 'padding-left', 'padding-right', 'box-sizing'
    ];

    function init() {
        const root = document.querySelector('.avatar');
        const email = document.querySelector('[data-avatar-email]');
        const passwords = [...document.querySelectorAll('[data-avatar-password]')];
        const toggle = document.querySelector('[data-avatar-toggle]');

        if (!root || !email || passwords.length === 0) return;

        if (!window.gsap || !window.MorphSVGPlugin) {
            console.warn('[avatar-login] Faltan gsap o MorphSVGPlugin.');
            return;
        }

        // Respeta la preferencia del sistema: el avatar queda estático.
        if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;

        gsap.registerPlugin(MorphSVGPlugin);

        const $ = (selector) => root.querySelector(selector);
        const el = {
            eyeL: $('.eyeL'),
            eyeR: $('.eyeR'),
            nose: $('.nose'),
            mouth: $('.mouth'),
            mouthBG: $('.mouthBG'),
            mouthSmallBG: $('.mouthSmallBG'),
            mouthMediumBG: $('.mouthMediumBG'),
            mouthLargeBG: $('.mouthLargeBG'),
            mouthMaskPath: $('#mouthMaskPath'),
            mouthOutline: $('.mouthOutline'),
            tooth: $('.tooth'),
            tongue: $('.tongue'),
            chin: $('.chin'),
            face: $('.face'),
            eyebrow: $('.eyebrow'),
            hair: $('.hair'),
            outerEarL: $('.earL .outerEar'),
            outerEarR: $('.earR .outerEar'),
            earHairL: $('.earL .earHair'),
            earHairR: $('.earR .earHair'),
            twoFingers: $('.twoFingers'),
            armL: $('.armL'),
            armR: $('.armR'),
            bodyBG: $('.bodyBGnormal'),
            bodyBGchanged: $('.bodyBGchanged')
        };
        const bodyNormalPath = el.bodyBG.getAttribute('d');
        const eyes = [el.eyeL, el.eyeR];
        const mouthShapes = [el.mouthBG, el.mouthOutline, el.mouthMaskPath];

        // Misma duración y suavizado para casi todo; overwrite evita que se pisen tweens.
        const tween = (targets, vars) =>
            gsap.to(targets, { duration: 1, ease: 'expo.out', overwrite: 'auto', ...vars });

        const pagePos = (node) => {
            const r = node.getBoundingClientRect();
            return { x: r.left + window.scrollX, y: r.top + window.scrollY };
        };

        /* ---------- Mirada: sigue el cursor de escritura del email ---------- */

        // Mide dónde está el cursor dentro del input copiando el texto en un div oculto.
        function caretOffsetX(styles) {
            let pos = null;
            try { pos = email.selectionEnd; } catch { /* input type=email en algunos navegadores */ }
            if (!pos) pos = email.value.length;

            const mirror = document.createElement('div');
            const marker = document.createElement('span');
            MIRROR_PROPS.forEach((p) => mirror.style.setProperty(p, styles.getPropertyValue(p)));
            Object.assign(mirror.style, {
                position: 'absolute', top: '0', left: '-9999px',
                visibility: 'hidden', whiteSpace: 'pre'
            });
            mirror.textContent = email.value.slice(0, pos);
            marker.textContent = email.value.slice(pos) || '.';
            mirror.append(marker);
            document.body.append(mirror);

            const x = marker.offsetLeft + (parseFloat(styles.borderLeftWidth) || 0);
            mirror.remove();
            return x;
        }

        function lookAtCaret() {
            // Se mide en cada llamada para tolerar resize y cambios de layout.
            const svg = pagePos(root);
            const scale = root.offsetWidth / SVG_SIZE;
            const part = (x, y) => ({ x: svg.x + x * scale, y: svg.y + y * scale });
            const parts = {
                eyeL: part(84, 76), eyeR: part(113, 76),
                nose: part(97, 81), mouth: part(100, 100)
            };
            const center = svg.x + root.offsetWidth / 2;

            const emailPos = pagePos(email);
            const styles = getComputedStyle(email);
            const caretX = email.scrollWidth <= email.clientWidth
                ? caretOffsetX(styles)
                : email.clientWidth;

            const targetX = emailPos.x + caretX;
            const targetY = emailPos.y + email.offsetHeight * 0.4;
            const angle = (p) => Math.atan2(p.y - targetY, p.x - targetX);

            const eyeLAngle = angle(parts.eyeL);
            const eyeRAngle = angle(parts.eyeR);
            const noseAngle = angle(parts.nose);
            const mouthAngle = angle(parts.mouth);

            const eyeLX = Math.cos(eyeLAngle) * 20;
            const eyeLY = Math.sin(eyeLAngle) * 10;
            const eyeRX = Math.cos(eyeRAngle) * 20;
            const eyeRY = Math.sin(eyeRAngle) * 10;
            const noseX = Math.cos(noseAngle) * 23;
            const noseY = Math.sin(noseAngle) * 10;
            const mouthX = Math.cos(mouthAngle) * 23;
            const mouthY = Math.sin(mouthAngle) * 10;
            const mouthR = Math.cos(mouthAngle) * 6;
            const chinX = mouthX * 0.8;
            const chinY = mouthY * 0.5;
            const faceX = mouthX * 0.3;
            const faceY = mouthY * 0.4;
            const faceSkew = Math.cos(mouthAngle) * 5;
            const eyebrowSkew = Math.cos(mouthAngle) * 25;
            const outerEarX = Math.cos(mouthAngle) * 4;
            const outerEarY = Math.cos(mouthAngle) * 5;
            const hairX = Math.cos(mouthAngle) * 6;

            let chinS = 1 - ((center - targetX) * 0.15) / 100;
            if (chinS > 1) chinS = Math.max(1 - (chinS - 1), 0.5);

            tween(el.eyeL, { x: -eyeLX, y: -eyeLY });
            tween(el.eyeR, { x: -eyeRX, y: -eyeRY });
            tween(el.nose, { x: -noseX, y: -noseY, rotation: mouthR, transformOrigin: 'center center' });
            tween(el.mouth, { x: -mouthX, y: -mouthY, rotation: mouthR, transformOrigin: 'center center' });
            tween(el.chin, { x: -chinX, y: -chinY, scaleY: chinS });
            tween(el.face, { x: -faceX, y: -faceY, skewX: -faceSkew, transformOrigin: 'center top' });
            tween(el.eyebrow, { x: -faceX, y: -faceY, skewX: -eyebrowSkew, transformOrigin: 'center top' });
            tween(el.outerEarL, { x: outerEarX, y: -outerEarY });
            tween(el.outerEarR, { x: outerEarX, y: outerEarY });
            tween(el.earHairL, { x: -outerEarX, y: -outerEarY });
            tween(el.earHairR, { x: -outerEarX, y: outerEarY });
            tween(el.hair, { x: hairX, scaleY: 1.2, transformOrigin: 'center bottom' });
        }

        function resetFace() {
            tween(eyes, { x: 0, y: 0 });
            tween(el.nose, { x: 0, y: 0, scaleX: 1, scaleY: 1 });
            tween(el.mouth, { x: 0, y: 0, rotation: 0 });
            tween(el.chin, { x: 0, y: 0, scaleY: 1 });
            tween([el.face, el.eyebrow], { x: 0, y: 0, skewX: 0 });
            tween([el.outerEarL, el.outerEarR, el.earHairL, el.earHairR, el.hair], { x: 0, y: 0, scaleY: 1 });
        }

        /* ---------- Boca y ojos según lo escrito ---------- */

        const MOUTH = {
            small: { shape: el.mouthSmallBG, eyes: 1, tooth: { x: 0, y: 0 }, tongueY: 0 },
            medium: { shape: el.mouthMediumBG, eyes: 0.85, tooth: { x: 0, y: 0 }, tongueY: 1 },
            large: { shape: el.mouthLargeBG, eyes: 0.65, tooth: { x: 3, y: -2 }, tongueY: 2 }
        };
        let mouthState = 'small';
        let eyeScale = 1;

        function setMouthState(next) {
            if (next === mouthState) return;
            const cfg = MOUTH[next];
            const shapeIndex = mouthState === 'small' && next === 'medium' ? 8
                : next === 'small' ? 9
                    : 'auto';
            mouthState = next;
            eyeScale = cfg.eyes;

            tween(mouthShapes, { morphSVG: { shape: cfg.shape, shapeIndex } });
            tween(el.tooth, cfg.tooth);
            tween(el.tongue, { y: cfg.tongueY });
            tween(eyes, { scaleX: cfg.eyes, scaleY: cfg.eyes, transformOrigin: 'center center' });
        }

        function onEmailInput() {
            lookAtCaret();
            const value = email.value;
            setMouthState(value.length === 0 ? 'small' : value.includes('@') ? 'large' : 'medium');
        }

        /* ---------- Brazos (tapan los ojos en el campo de contraseña) ---------- */

        let eyesCovered = false;

        function coverEyes() {
            gsap.killTweensOf([el.armL, el.armR]);
            gsap.set([el.armL, el.armR], { visibility: 'visible' });
            gsap.to(el.armL, { duration: 0.45, x: -93, y: 10, rotation: 0, ease: 'quad.out' });
            gsap.to(el.armR, { duration: 0.45, x: -93, y: 10, rotation: 0, ease: 'quad.out', delay: 0.1 });
            gsap.to(el.bodyBG, { duration: 0.45, morphSVG: { shape: el.bodyBGchanged }, ease: 'quad.out' });
            eyesCovered = true;
        }

        function uncoverEyes() {
            gsap.killTweensOf([el.armL, el.armR]);
            gsap.to(el.armL, { duration: 1.35, y: 220, ease: 'quad.out' });
            gsap.to(el.armL, { duration: 1.35, rotation: 105, ease: 'quad.out', delay: 0.1 });
            gsap.to(el.armR, { duration: 1.35, y: 220, ease: 'quad.out' });
            gsap.to(el.armR, {
                duration: 1.35, rotation: -105, ease: 'quad.out', delay: 0.1,
                onComplete: () => gsap.set([el.armL, el.armR], { visibility: 'hidden' })
            });
            gsap.to(el.bodyBG, { duration: 0.45, morphSVG: { shape: bodyNormalPath }, ease: 'quad.out' });
            eyesCovered = false;
        }

        const spreadFingers = () => gsap.to(el.twoFingers, {
            duration: 0.35, transformOrigin: 'bottom left', rotation: 30, x: -9, y: -2, ease: 'power2.inOut'
        });
        const closeFingers = () => gsap.to(el.twoFingers, {
            duration: 0.35, transformOrigin: 'bottom left', rotation: 0, x: 0, y: 0, ease: 'power2.inOut'
        });

        /* ---------- Foco: decide qué hace el avatar según el control activo ---------- */

        let emailFocused = false;
        let pointerOnToggle = false; // evita bajar los brazos mientras se hace clic en el checkbox

        function update() {
            if (pointerOnToggle) return;
            const active = document.activeElement;
            const inPassword = passwords.includes(active) || (toggle !== null && active === toggle);

            if (inPassword && !eyesCovered) coverEyes();
            else if (!inPassword && eyesCovered) uncoverEyes();

            if (active === email) {
                emailFocused = true;
                onEmailInput();
            } else if (emailFocused) {
                emailFocused = false;
                resetFace();
            }
        }

        /* ---------- Parpadeo ---------- */

        function blink() {
            gsap.to(eyes, {
                duration: 0.1, scaleY: 0, yoyo: true, repeat: 1, transformOrigin: 'center center',
                overwrite: false,
                onComplete: () => gsap.set(eyes, { scaleY: eyeScale })
            });
        }

        function scheduleBlink(maxDelay) {
            gsap.delayedCall(gsap.utils.random(1, maxDelay), () => {
                blink();
                scheduleBlink(12);
            });
        }

        /* ---------- Arranque ---------- */

        gsap.set(el.armL, { x: -93, y: 220, rotation: 105, transformOrigin: 'top left' });
        gsap.set(el.armR, { x: -93, y: 220, rotation: -105, transformOrigin: 'top right' });
        gsap.set(el.mouth, { transformOrigin: 'center center' });
        scheduleBlink(5);

        email.addEventListener('input', onEmailInput);
        document.addEventListener('focusin', update);
        document.addEventListener('focusout', () => setTimeout(update, 50));

        if (toggle) {
            const onToggle = () => {
                passwords.forEach((p) => { p.type = toggle.checked ? 'text' : 'password'; });
                if (toggle.checked) spreadFingers(); else closeFingers();
            };
            const zone = toggle.closest('label') ?? toggle;

            toggle.addEventListener('change', onToggle);
            toggle.addEventListener('click', () => toggle.focus()); // Safari no enfoca checkboxes al hacer clic
            zone.addEventListener('pointerdown', () => { pointerOnToggle = true; });
            document.addEventListener('pointerup', () => {
                if (!pointerOnToggle) return;
                pointerOnToggle = false;
                setTimeout(update, 50);
            });
            onToggle();
        }

        update(); // cubre un campo con autofocus
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
document.querySelector('.login-form')?.addEventListener('submit', (e) => {
    e.currentTarget.querySelector('button[type="submit"]').disabled = true;
});
