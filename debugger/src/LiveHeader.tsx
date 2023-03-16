import { createEffect, createSignal } from "solid-js";
import { liveDataState, SendMessageToServer } from "./App";

export interface LiveData {
    fps: number;
    runtime: number;
    connected: boolean;
    hierarchy: any;
    updateRate: number;
}

let interval: number;
export default function LiveHeader() {
    let [runtime, setRuntime] = createSignal("N/A");
    createEffect(() => {
        if (liveDataState.runtime === 0) {
            setRuntime("N/A");
            return;
        }
        let start = new Date().getTime() - liveDataState.runtime;
        if (interval !== undefined) {
            clearInterval(interval);
        }
        interval = setInterval(() => {
            if (!liveDataState.connected) {
                clearInterval(interval);
                return;
            }
            let millis = new Date().getTime() - start;
            setRuntime(
                millis > 1000 ? (millis / 1000).toFixed(2) + "s" : millis + "ms"
            );
        }, 100);
        return liveDataState;
    });
    return (
        <div
            class={"liveheader" + (!liveDataState.connected ? " offline" : "")}
        >
            <div
                class={
                    "live " +
                    (liveDataState.connected ? "interactable" : "offline")
                }
                onClick={() => SendMessageToServer("showwindow")}
            >
                {liveDataState.connected ? "Live" : "Offline"}
            </div>
            <div>
                FPS:{" "}
                {liveDataState.fps > 999
                    ? 999
                    : liveDataState.fps.toString().padStart(3, "0")}
            </div>
            <div>
                Tick Rate:
                {" " + liveDataState.updateRate.toString().padStart(4, "0")}
            </div>
            <div>Runtime: {runtime}</div>
            <div
                class={"" + (liveDataState.connected && "interactable")}
                onClick={() => SendMessageToServer("reloadshaders")}
            >
                Reload Shaders
            </div>
            <div
                class={"" + (liveDataState.connected && "interactable")}
                onClick={() => SendMessageToServer("quit")}
            >
                Quit
            </div>
        </div>
    );
}
