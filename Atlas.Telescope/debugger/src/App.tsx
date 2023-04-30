import { Component, createEffect, createSignal, Show } from "solid-js";
import { createStore } from "solid-js/store";
import styles from "./App.module.css";
import Debugger, {
  DebuggerData,
  Log,
  LogType,
  UpdateTickDurations,
} from "./Debugger";
import { LiveData } from "./LiveHeader";
import { addData } from "./Profiler";
import Loading from "./Loading";
export const [debuggerState, setDebuggerState] = createStore<DebuggerData>({
  runDate: "N/A",
  engineVersion: "Unknown version",
  live: false,
} as DebuggerData);
export const [logState, setLogState] = createStore<Log[]>([]);
export const [subSystemsState, setSubSystemState] = createStore<string[]>([]);
export const [liveDataState, setLiveDataState] = createStore<LiveData>({
  globalData: ["Waiting for connection..."],
  connected: false,
  hierarchy: { name: "ROOT", components: [], children: [] },
} as LiveData);
let sendMessage: (data: string) => void = (data: string) => {};
export function SendMessageToServer(message: string) {
  sendMessage(message);
}
const App: Component = () => {
  createEffect(() => {
    let data = parseData();
    if (data.live === true) {
      StartListening();
    } else {
      setDebuggerState(data);
    }
  });
  return (
    <div class={styles.App}>
      <Show when={debuggerState.live && !liveDataState.connected}>
        <Loading></Loading>
      </Show>
      <Debugger></Debugger>
    </div>
  );
};

function StartListening() {
  let socket: WebSocket;
  let returned = false;
  let opened = false;
  console.log("Trying to connect");
  try {
    socket = new WebSocket("ws://localhost:8989/ws");
  } catch (e) {
    return;
  }
  socket.onopen = () => {
    opened = true;
    if (returned) {
      socket.close();
      return;
    }
    sendMessage = (data) => {
      socket.send(data);
    };
    setLiveDataState({ connected: true });
  };
  setTimeout(() => {
    if (!opened) socket.close();
  }, 1000);
  socket.onerror = (e: Event) => {
    if (returned) {
      return;
    }
    setLiveDataState({ connected: false });
    sendMessage = (data: string) => {};
    setTimeout(StartListening, 500);
    returned = true;
  };
  socket.onclose = () => {
    if (returned) {
      return;
    }
    sendMessage = (data: string) => {};
    setLiveDataState({ connected: false });
    setTimeout(StartListening, 1000);
    returned = true;
  };
  socket.addEventListener("message", function (event) {
    let data = JSON.parse(event.data);
    if (data !== undefined && data.type !== undefined) {
      if (data.type === "log" && data.content !== undefined) {
        if (logState !== null) {
          let log = parseLine(data.content);
          if (log !== null) {
            setLogState([...logState, log]);
          }
        }
      } else if (data.type === "livedata") {
        setLiveDataState({
          globalData: data.globalData,
          hierarchy: data.hierarchy,
        });
      } else if (data.type === "profiler" && data.times !== undefined) {
        addData(data.times["Update"]);
        if (data.times["Tick"] == undefined) return;
        UpdateTickDurations(
          Object.values(data.times["Tick"]),
          Object.keys(data.times["Tick"])
        );
      }
    }
  });
}

function parseData(): DebuggerData {
  let data = document.getElementById("logs")?.innerText;
  if (data === undefined || data === "") {
    console.log("Running in live mode");
    setDebuggerState({ live: true });
    return {
      runDate: "N/A",
      engineVersion: "Unknown version",
      live: true,
    } as DebuggerData;
  }
  let lines = data.split("\n");
  let logs = lines.splice(3, lines.length - 3);
  let parsedLogs = [];
  let subSystems: string[] = [];
  for (let i = 0; i < logs.length; i++) {
    let log = parseLine(logs[i]);
    if (log !== null) parsedLogs.push(log);
  }
  setLogState(parsedLogs);
  document.title = "Atlas | Log Viewer";

  return {
    runDate: lines[1].replace("RUN_DATE: ", ""),
    engineVersion: lines[0].replace("ENGINE_VERSION: ", ""),
    live: false,
  } as DebuggerData;
}

function parseLine(log: string) {
  let matches = log.matchAll(/\[(.*?)\]/g);
  let val = matches.next().value;
  let date = "";
  if (val !== undefined) {
    date = val[1];
  }
  val = matches.next().value;
  let type = LogType.Info;
  if (val !== undefined) {
    switch (val[1]) {
      case "INFO":
        type = LogType.Info;
        break;
      case "WARN":
        type = LogType.Warning;
        break;
      case "ERROR":
        type = LogType.Error;
        break;
      default:
        break;
    }
  }

  // Next we need to parse out the subsystem
  let m = log.matchAll(/\[.*?\] \[.*?\] (.*?) > (.*)/g);
  let value = m.next().value;
  let subSystem = "";
  let content = "";
  if (value !== undefined) {
    subSystem = value[1];
    content = value[2];
  }
  if (date === "") {
    return null;
  }
  if (!subSystemsState.includes(subSystem) && subSystem !== "") {
    setSubSystemState([...subSystemsState, subSystem]);
  }
  return {
    date: date,
    type: type,
    subSystem: subSystem,
    content: content,
  } as Log;
}

export default App;
