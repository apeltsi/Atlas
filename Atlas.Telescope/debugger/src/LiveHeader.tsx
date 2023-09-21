import {For} from "solid-js";
import {liveDataState, SendMessageToServer} from "./App";

export interface LiveData {
    globalData: string[];
    connected: boolean;
    hierarchy: any;
}

let interval: number;
export default function LiveHeader() {
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
            <For each={liveDataState.globalData}>
                {(item) => {
                    return (
                        <div
                            class={
                                "" + (liveDataState.connected && "interactable")
                            }
                        >
                            {item}
                        </div>
                    );
                }}
            </For>
            <div
                class={"" + (liveDataState.connected && "interactable")}
                onClick={() => SendMessageToServer("quit")}
            >
                Quit
            </div>
        </div>
    );
}
