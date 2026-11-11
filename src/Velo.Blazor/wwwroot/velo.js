// Velo UI - client helpers. Imported on demand by the components that need them.

let dndReady = false;

/**
 * HTML5 drag & drop needs dataTransfer.setData in Firefox, which Blazor can't call.
 * One document listener covers every element marked data-velo-drag.
 */
export function enableDnd() {
    if (dndReady) return;
    dndReady = true;
    document.addEventListener("dragstart", e => {
        if (e.target instanceof Element && e.target.closest("[data-velo-drag]")) {
            e.dataTransfer?.setData("text/plain", "");
            if (e.dataTransfer) e.dataTransfer.effectAllowed = "move";
        }
    });
}

/** Gives focus back to an element after Blazor recreated it (keyboard moves across containers). */
export function focusKey(root, key) {
    const el = root?.querySelector(`[data-velo-key="${CSS.escape(key)}"]`);
    el?.focus({ preventScroll: false });
}

/**
 * Timeline editing: move (and change lane) or resize items with the pointer, or with the keyboard.
 * The preview is purely visual; .NET is called once, on drop, and decides the final state.
 */
export function attachTimeline(root, dotnet, options) {
    let state = null;
    const snap = ms => Math.round(ms / options.snapMs) * options.snapMs;

    const editableItem = target =>
        target instanceof Element ? target.closest(".velo-timeline__item[data-editable]") : null;

    function onPointerDown(e) {
        if (e.button !== 0) return;
        const item = editableItem(e.target);
        if (!item || !root.contains(item)) return;

        const handle = e.target.closest("[data-handle]");
        const track = item.closest(".velo-timeline__track");
        state = {
            item,
            track,
            target: track,
            mode: handle ? handle.dataset.handle : "move",
            x0: e.clientX,
            y0: e.clientY,
            left: parseFloat(item.style.left),
            width: parseFloat(item.style.width),
            style: item.getAttribute("style"),
            trackWidth: track.getBoundingClientRect().width,
            deltaMs: 0,
            moved: false,
        };
        document.addEventListener("pointermove", onPointerMove);
        document.addEventListener("pointerup", onPointerUp, { once: true });
        document.addEventListener("pointercancel", cancel, { once: true });
    }

    function onPointerMove(e) {
        if (!state) return;
        const dx = e.clientX - state.x0;
        const dy = e.clientY - state.y0;
        if (!state.moved) {
            if (Math.hypot(dx, dy) < 4) return; // a click, not a drag
            state.moved = true;
            state.item.classList.add("is-dragging");
            root.classList.add("is-dragging");
        }

        const minPct = options.snapMs / options.spanMs * 100;
        const deltaMs = snap(dx / state.trackWidth * options.spanMs);
        const pct = deltaMs / options.spanMs * 100;
        let left = state.left;
        let width = state.width;
        if (state.mode === "move") {
            left += pct;
        } else if (state.mode === "start") {
            left += pct;
            width -= pct;
            if (width < minPct) { left -= minPct - width; width = minPct; }
        } else {
            width = Math.max(minPct, width + pct);
        }

        state.deltaMs = deltaMs;
        state.item.style.left = `${left}%`;
        state.item.style.width = `${width}%`;
        state.item.dataset.delta = deltaMs === 0 ? "" : formatDelta(deltaMs);

        if (state.mode === "move" && options.lanes) {
            const under = document.elementsFromPoint(e.clientX, e.clientY)
                .find(el => el.classList.contains("velo-timeline__track") && root.contains(el));
            if (under && under !== state.target) {
                state.target.classList.remove("is-drop-target");
                state.target = under;
                if (under !== state.track) under.classList.add("is-drop-target");
            }
            const offset = state.target.getBoundingClientRect().top - state.track.getBoundingClientRect().top;
            state.item.style.transform = offset ? `translateY(${offset}px)` : "";
        }
    }

    function reset() {
        document.removeEventListener("pointermove", onPointerMove);
        state.item.setAttribute("style", state.style); // let Blazor render the final position
        state.item.classList.remove("is-dragging");
        delete state.item.dataset.delta;
        state.target.classList.remove("is-drop-target");
        root.classList.remove("is-dragging");
    }

    function onPointerUp() {
        if (!state) return;
        const s = state;
        reset();
        state = null;
        if (!s.moved) return;

        // The click that follows a drag must not open the item.
        const swallow = ev => { ev.stopPropagation(); ev.preventDefault(); };
        window.addEventListener("click", swallow, { capture: true, once: true });
        setTimeout(() => window.removeEventListener("click", swallow, { capture: true }), 0);

        const lane = s.target !== s.track ? (s.target.dataset.lane ?? "") : null;
        if (s.deltaMs !== 0 || lane !== null) {
            dotnet.invokeMethodAsync("OnItemDragged", s.item.dataset.id, s.mode, s.deltaMs, lane);
        }
    }

    function cancel() {
        if (!state) return;
        reset();
        state = null;
    }

    function onKeyDown(e) {
        const item = editableItem(e.target);
        if (!item || !root.contains(item)) return;
        if (e.key === "Escape" && state) { cancel(); return; }
        if (e.key !== "ArrowLeft" && e.key !== "ArrowRight") return;
        e.preventDefault(); // otherwise the arrows scroll the timeline
        const delta = (e.key === "ArrowLeft" ? -1 : 1) * options.snapMs;
        dotnet.invokeMethodAsync("OnItemDragged", item.dataset.id, e.shiftKey ? "end" : "move", delta, null);
    }

    function formatDelta(ms) {
        const minutes = Math.round(ms / 60000);
        const sign = minutes > 0 ? "+" : "−";
        const abs = Math.abs(minutes);
        return abs >= 60 ? `${sign}${Math.floor(abs / 60)} h ${String(abs % 60).padStart(2, "0")}` : `${sign}${abs} min`;
    }

    root.addEventListener("pointerdown", onPointerDown);
    root.addEventListener("keydown", onKeyDown);

    return {
        update(next) { Object.assign(options, next); },
        dispose() {
            cancel();
            root.removeEventListener("pointerdown", onPointerDown);
            root.removeEventListener("keydown", onKeyDown);
        },
    };
}

/** Focuses the first focusable element inside a container (returning focus to a trigger). */
export function focusFirst(root) {
    root?.querySelector("button:not([disabled]), [href], input:not([disabled]), [tabindex]:not([tabindex='-1'])")?.focus();
}

/** Menu: focus the first item and move between items with the arrow keys, Home and End. */
export function menuOpen(list) {
    const items = () => [...list.querySelectorAll("[role=menuitem]:not([disabled])")];
    items()[0]?.focus();
    if (list.__veloMenu) return;
    list.__veloMenu = true;
    list.addEventListener("keydown", e => {
        const all = items();
        const i = all.indexOf(document.activeElement);
        let next = null;
        if (e.key === "ArrowDown") next = (i + 1) % all.length;
        else if (e.key === "ArrowUp") next = (i - 1 + all.length) % all.length;
        else if (e.key === "Home") next = 0;
        else if (e.key === "End") next = all.length - 1;
        if (next !== null) {
            e.preventDefault();
            all[next]?.focus();
        }
    });
}

/**
 * Splitter: drag the divider (live, client side) or use the arrow keys; .NET gets the final size.
 */
export function attachSplitter(root, dotnet, options) {
    const divider = root.querySelector(":scope > .velo-splitter__divider");
    const clamp = v => Math.min(options.max, Math.max(options.min, v));
    let dragging = false;
    let size = null;

    const sizeAt = e => {
        const r = root.getBoundingClientRect();
        return clamp(options.vertical ? (e.clientY - r.top) / r.height * 100 : (e.clientX - r.left) / r.width * 100);
    };
    const apply = v => {
        size = v;
        root.style.setProperty("--_size", `${v}%`);
        divider.setAttribute("aria-valuenow", Math.round(v));
    };
    const commit = () => size !== null && dotnet.invokeMethodAsync("OnResized", size);

    function onDown(e) {
        if (e.button !== 0) return;
        dragging = true;
        divider.setPointerCapture(e.pointerId);
        root.classList.add("is-resizing");
        e.preventDefault();
    }
    function onMove(e) { if (dragging) apply(sizeAt(e)); }
    function onUp() {
        if (!dragging) return;
        dragging = false;
        root.classList.remove("is-resizing");
        commit();
    }
    function onKey(e) {
        const back = options.vertical ? "ArrowUp" : "ArrowLeft";
        const fwd = options.vertical ? "ArrowDown" : "ArrowRight";
        const current = parseFloat(getComputedStyle(root).getPropertyValue("--_size")) || options.min;
        let next = null;
        if (e.key === back) next = current - options.step;
        else if (e.key === fwd) next = current + options.step;
        else if (e.key === "Home") next = options.min;
        else if (e.key === "End") next = options.max;
        if (next === null) return;
        e.preventDefault();
        apply(clamp(next));
        commit();
    }

    divider.addEventListener("pointerdown", onDown);
    divider.addEventListener("pointermove", onMove);
    divider.addEventListener("pointerup", onUp);
    divider.addEventListener("lostpointercapture", onUp);
    divider.addEventListener("keydown", onKey);
    return {
        update(next) { Object.assign(options, next); },
        dispose() {
            divider.removeEventListener("pointerdown", onDown);
            divider.removeEventListener("pointermove", onMove);
            divider.removeEventListener("pointerup", onUp);
            divider.removeEventListener("lostpointercapture", onUp);
            divider.removeEventListener("keydown", onKey);
        },
    };
}

/** Stops the browser's default action (scrolling) for these keys only; .NET still receives them. */
export function preventKeys(el, keys) {
    if (!el || el.__veloKeys) return;
    el.__veloKeys = true;
    el.addEventListener("keydown", e => {
        if (keys.includes(e.key)) e.preventDefault();
    });
}

/**
 * Carousel: native scroll-snap does the swiping; this reports the current slide to .NET
 * and scrolls to a slide on demand.
 */
export function attachCarousel(track, dotnet) {
    let last = -1;
    let timer = null;
    const slides = () => [...track.querySelectorAll(":scope > .velo-carousel__slide")];
    const current = () => {
        const list = slides();
        if (list.length === 0) return 0;
        const left = track.scrollLeft;
        let best = 0;
        let bestDist = Infinity;
        list.forEach((s, i) => {
            const d = Math.abs(s.offsetLeft - track.offsetLeft - left);
            if (d < bestDist) { bestDist = d; best = i; }
        });
        return best;
    };
    const onScroll = () => {
        clearTimeout(timer);
        timer = setTimeout(() => {
            const i = current();
            if (i !== last) {
                last = i;
                dotnet.invokeMethodAsync("OnSlideChanged", i);
            }
        }, 90);
    };
    track.addEventListener("scroll", onScroll, { passive: true });
    return {
        goTo(index, smooth) {
            const s = slides()[index];
            if (!s) return;
            const reduce = matchMedia("(prefers-reduced-motion: reduce)").matches;
            track.scrollTo({ left: s.offsetLeft - track.offsetLeft, behavior: smooth && !reduce ? "smooth" : "auto" });
        },
        count() { return slides().length; },
        dispose() { track.removeEventListener("scroll", onScroll); clearTimeout(timer); },
    };
}

/**
 * Color picker saturation/value square: drag (live, throttled to one .NET call per frame) and arrow keys.
 */
export function attachColorArea(area, dotnet) {
    let dragging = false;
    let pending = null;
    let frame = 0;
    const thumb = () => area.querySelector(".velo-color__area-thumb");
    const send = () => {
        frame = 0;
        if (pending) dotnet.invokeMethodAsync("OnArea", pending.s, pending.v);
        pending = null;
    };
    const at = e => {
        const r = area.getBoundingClientRect();
        const s = Math.min(1, Math.max(0, (e.clientX - r.left) / r.width));
        const v = Math.min(1, Math.max(0, 1 - (e.clientY - r.top) / r.height));
        const t = thumb();
        if (t) { t.style.left = `${s * 100}%`; t.style.top = `${(1 - v) * 100}%`; }
        pending = { s, v };
        if (!frame) frame = requestAnimationFrame(send);
    };
    const down = e => { if (e.button !== 0) return; dragging = true; area.setPointerCapture(e.pointerId); at(e); e.preventDefault(); area.focus(); };
    const move = e => { if (dragging) at(e); };
    const up = () => { dragging = false; };
    const key = e => {
        const step = e.shiftKey ? 0.1 : 0.02;
        const d = { ArrowLeft: [-step, 0], ArrowRight: [step, 0], ArrowUp: [0, step], ArrowDown: [0, -step] }[e.key];
        if (!d) return;
        e.preventDefault();
        dotnet.invokeMethodAsync("OnAreaStep", d[0], d[1]);
    };
    area.addEventListener("pointerdown", down);
    area.addEventListener("pointermove", move);
    area.addEventListener("pointerup", up);
    area.addEventListener("lostpointercapture", up);
    area.addEventListener("keydown", key);
    return {
        dispose() {
            area.removeEventListener("pointerdown", down);
            area.removeEventListener("pointermove", move);
            area.removeEventListener("pointerup", up);
            area.removeEventListener("lostpointercapture", up);
            area.removeEventListener("keydown", key);
        },
    };
}

/** Screen color picker (Chromium only). Returns "#rrggbb", or null if unsupported or cancelled. */
export async function eyeDropper() {
    if (!("EyeDropper" in window)) return null;
    try {
        const result = await new window.EyeDropper().open();
        return result.sRGBHex;
    } catch {
        return null;
    }
}

export function hasEyeDropper() { return "EyeDropper" in window; }
