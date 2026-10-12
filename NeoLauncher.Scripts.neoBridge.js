(() => {
    if (window.__neoBridgeInstalled) return;
    window.__neoBridgeInstalled = true;

    const wv = window.chrome && window.chrome.webview;
    const pending = new Map();
    let seq = 0;

    function invoke(command, payload) {
        return new Promise((resolve, reject) => {
            if (!wv) { reject(new Error("Neo bridge unavailable")); return; }
            const id = "n" + (++seq);
            pending.set(id, { resolve, reject });
            wv.postMessage(JSON.stringify({ kind: "invoke", id, command, payload: payload == null ? {} : payload }));
        });
    }

    if (wv) {
        wv.addEventListener("message", (e) => {
            let msg = e.data;
            if (typeof msg === "string") { try { msg = JSON.parse(msg); } catch { return; } }
            if (!msg || typeof msg !== "object") return;

            if (msg.kind === "result") {
                const p = pending.get(msg.id);
                if (!p) return;
                pending.delete(msg.id);
                if (msg.ok) p.resolve(msg.result);
                else p.reject(new Error(msg.error || "Neo bridge error"));
                return;
            }

            if (msg.kind === "event") {
                try {
                    if (msg.event === "oauth-callback") {
                        if (typeof window.NeoHandleOAuthCallback === "function") window.NeoHandleOAuthCallback(msg.detail);
                        window.dispatchEvent(new CustomEvent("neo-auth-callback", { detail: msg.detail }));
                        window.postMessage({ type: "neo-auth-callback", url: msg.detail }, "*");
                    } else {
                        window.dispatchEvent(new CustomEvent(msg.event, { detail: msg.detail }));
                    }
                } catch (err) { console.error("Neo event dispatch failed", err); }
            }
        });
    }

    // Any property access becomes a command call, so the web UI's bridge-first paths
    // (getNeoBridge / invokeNeoAuthBridge) always route here instead of falling back to Tauri.
    const RESERVED = new Set(["then", "catch", "finally", "toJSON", "constructor", "prototype"]);
    const services = new Proxy({}, {
        get(_t, prop) {
            if (typeof prop !== "string" || RESERVED.has(prop)) return undefined;
            return (payload) => invoke(prop, payload);
        }
    });
    window.neoLauncherServices = services;
    window.NeoLauncherServices = services;

    function neoComputeDragRects() {
        const H = 50, W = window.innerWidth;
        // Full-screen invisible "click outside to close" buttons (Manage/Modifiers
        // panel, item picker, opened build view, trailer/article modals) are real
        // <button> elements spanning the entire window, so without this exclusion
        // they show up as a single blocker covering [0, W] and swallow every gap
        // in the title-bar strip above — the whole window becomes undraggable the
        // moment any such overlay is open. Elements (or their overlay ancestor)
        // marked data-neo-drag-passthrough opt out of counting as a blocker here;
        // they still work as click-catchers everywhere except the strip itself.
        const blockers = [...document.querySelectorAll(
            'button,a,input,select,textarea,[role="button"],[data-neo-no-drag="true"]')]
            .filter(el => !el.closest('[data-neo-drag-passthrough="true"]'))
            .map(el => el.getBoundingClientRect())
            .filter(r => r.top < H && r.bottom > 0 && r.width > 0)
            .sort((a, b) => a.left - b.left);
        const gaps = []; let x = 0;
        for (const r of blockers) {
            const L = Math.max(0, r.left), R = Math.min(W, r.right);
            if (L > x) gaps.push({ x: Math.round(x), w: Math.round(L - x) });
            x = Math.max(x, R);
        }
        if (x < W) gaps.push({ x: Math.round(x), w: Math.round(W - x) });
        return gaps;
    }
    function neoComputeInteractiveRects() {
        return [...document.querySelectorAll(
            'button,a,input,select,textarea,[role="button"],[data-neo-no-drag="true"]'
        )]
            .map(el => el.getBoundingClientRect())
            .map(r => ({
                x: Math.round(r.left),
                y: Math.round(r.top),
                w: Math.round(r.width),
                h: Math.round(r.height)
            }));
    }
    let _neoDragRects = "";
    function neoReportDragRects() {
        try {
            const dragRects = neoComputeDragRects();
            const interactiveRects = neoComputeInteractiveRects();

            const dragJson = JSON.stringify(dragRects);
            const interactiveJson = JSON.stringify(interactiveRects);

            if (dragJson !== _neoDragRects) {
                _neoDragRects = dragJson;

                invoke("neo_set_drag_rects", { rects: dragRects });
            }
        } catch { }
    }
    // Observe the ALWAYS-present root, never a maybe-null node (this was your crash):
    try {
        const ro = new ResizeObserver(() => neoReportDragRects());
        ro.observe(document.documentElement);
    } catch { }
    window.addEventListener("resize", neoReportDragRects);
    // catch title-text swaps (centered cluster width changes) without measuring sizes:
    let _rafPump = () => { neoReportDragRects(); requestAnimationFrame(_rafPump); };
    requestAnimationFrame(_rafPump);

    // The minimize/close buttons call Tauri's window API (a no-op here); route them to the host.
    document.addEventListener("click", (e) => {
        const btn = e.target && e.target.closest && e.target.closest("button[aria-label]");
        if (!btn) return;
        const label = (btn.getAttribute("aria-label") || "").toLowerCase();
        if (label === "minimize") invoke("neo_window_minimize");
        else if (label === "close") invoke("neo_window_close");
    }, true);
})();