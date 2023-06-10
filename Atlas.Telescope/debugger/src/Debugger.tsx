import { createEffect, createSignal, For, Show } from "solid-js";
import info from "./info.png";
import warn from "./warn.png";
import error from "./error.png";
import LiveHeader, { LiveData } from "./LiveHeader";
import {
  debuggerState,
  liveDataState,
  logState,
  setLogState,
  subSystemsState,
} from "./App";
import Profiler from "./Profiler";
import Hierarchy from "./Hierarchy";
import { DurationDisplay } from "./DurationDisplay";
const [durations, setDurations] = createSignal<number[]>([]);
const [names, setNames] = createSignal<string[]>([]);
export function UpdateTickDurations(td: number[], tn: string[]) {
  setDurations(td);
  setNames(tn);
}
export interface DebuggerData {
  runDate: string;
  engineVersion: string;
  live: boolean;
}
export enum LogType {
  Info,
  Warning,
  Error,
}
export interface Log {
  type: LogType;
  date: string;
  subSystem: string;
  content: string;
}
export default function Debugger() {
  let [filter, setFilters] = createSignal<string[]>([]);
  let [autoScroll, setAutoScroll] = createSignal(true);
  let logContainer: HTMLDivElement | undefined;
  createEffect((prevState: Log[]) => {
    if (prevState.length != logState.length) console.log(logContainer);
    if (logContainer !== undefined && autoScroll()) {
      logContainer.scroll(0, logContainer.scrollHeight);
    }
    return logState;
  }, []);
  return (
    <div id="debugger">
      <Show when={debuggerState.live}>
        <LiveHeader></LiveHeader>
        <Profiler></Profiler>
        <DurationDisplay
          durations={durations()!}
          names={names()!}
        ></DurationDisplay>
        <Hierarchy hierarchy={liveDataState.hierarchy}></Hierarchy>
      </Show>
      <Show when={!debuggerState.live}>
        <h2>Info</h2>
        <h3>Atlas version {debuggerState.engineVersion}</h3>
        <h3>Atlas started at: {debuggerState.runDate}</h3>
      </Show>

      <h2>Logs ({logState.length})</h2>
      <div id="log-actions">
        <div id="filters">
          <button
            onclick={() => {
              if (filter().length === subSystemsState.length) {
                setFilters([]);
              } else {
                setFilters([...subSystemsState]);
              }
            }}
            class={
              filter().length === subSystemsState.length ||
              filter().length === 0
                ? "selected"
                : ""
            }
          >
            All
          </button>
          <For each={subSystemsState}>
            {(item: string) => (
              <button
                onclick={() => {
                  let filters = filter();
                  if (filters.includes(item)) {
                    filters.splice(filters.indexOf(item), 1);
                    setFilters([...filters]);
                  } else {
                    setFilters([...filters, item]);
                  }
                }}
                class={filter().includes(item) ? "selected" : ""}
              >
                {item}
              </button>
            )}
          </For>
        </div>
        <span>---</span>
        <button
          onClick={() => {
            setLogState([]);
          }}
          class={"button-only"}
        >
          Clear Logs
        </button>
        <button
          onClick={() => {
            setAutoScroll(!autoScroll());
          }}
          class={autoScroll() ? "selected" : "toggle"}
        >
          Auto-Scroll
        </button>
      </div>
      <div
        id="logList"
        class={debuggerState.live ? "islive" : ""}
        ref={logContainer}
      >
        <For each={logState}>
          {(item: Log) => (
            <Show
              when={filter().length === 0 || filter().includes(item.subSystem)}
            >
              <div class={"log" + " " + getTypeName(item.type)}>
                <img src={getImage(item.type)} />
                <span>{item.date}</span>
                <span class="subsystem">{item.subSystem}</span>
                <span>{item.content}</span>
              </div>
            </Show>
          )}
        </For>
      </div>
    </div>
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
