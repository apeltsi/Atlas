import { createSignal, For, Show } from "solid-js";
import info from "./info.png";
import warn from "./warn.png";
import error from "./error.png";
import LiveHeader, { LiveData } from "./LiveHeader";
import { debuggerState, liveDataState, logState, subSystemsState } from "./App";
import Profiler from "./Profiler";

export interface DebuggerData {
    runDate: string,
    engineVersion: string,
    live: boolean
}
export enum LogType {
    Info,
    Warning,
    Error
}
export interface Log {
    type: LogType,
    date: string,
    subSystem: string,
    content: string
}
export default function Debugger() {
    let [filter, setFilters] = createSignal<string[]>([]);
    return (
        <div id="debugger">
            <Show when={debuggerState.live}>
                <LiveHeader></LiveHeader>
                <Profiler></Profiler>
            </Show>
            <Show when={!debuggerState.live}>
                <h2>Info</h2>
                <h3>Caerus version {debuggerState.engineVersion}</h3>
                <h3>Caerus started at: {debuggerState.runDate}</h3>
            </Show>

            <h2>Logs</h2>
            <div id="filters">
                <div onclick={() => {
                    if (filter().length === subSystemsState.length) {
                        setFilters([]);
                    } else {
                        setFilters([...subSystemsState]);
                    }
                }} class={filter().length === subSystemsState.length || filter().length === 0 ? "selected" : ""}>
                    All
                </div>
                <For each={subSystemsState}>{(item: string) => (<div onclick={() => {
                    let filters = filter();
                    if (filters.includes(item)) {
                        filters.splice(filters.indexOf(item), 1);
                        setFilters([...filters]);
                    } else {
                        setFilters([...filters, item]);
                    }
                }} class={filter().includes(item) ? "selected" : ""}>{item}</div>)}</For></div>
            <div id="logList">
                <For each={logState}>{
                    (item: Log) => (
                        <Show when={(filter().length === 0 || filter().includes(item.subSystem))}>
                            <div class={"log" + " " + getTypeName(item.type)}>
                                <img src={getImage(item.type)} />
                                <span>{item.date}</span>
                                <span class="subsystem">{item.subSystem}</span>
                                <span>{item.content}</span>

                            </div>
                        </Show>
                    )
                }</For>
            </div>
        </div >
    );
}
function getTypeName(type: LogType): string {
    switch (type) {
        case LogType.Info:
            return "info";
        case LogType.Warning:
            return "warn";
        case LogType.Error:
            return "error";
        default:
            return "info";

    }
}
function getImage(type: LogType): string {
    switch (type) {
        case LogType.Info:
            return info;
        case LogType.Warning:
            return warn;
        case LogType.Error:
            return error;
        default:
            return info;

    }
}
