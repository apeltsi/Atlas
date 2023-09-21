import {createEffect, createSignal, For, Setter} from "solid-js";
import styles from "./Hierarchy.module.css";

interface IHierarchyElement {
    name: string;
    components: {
        name: string;
        fields: { name: string; value: string; type: string }[];
    }[];
    children: IHierarchyElement[];
}

export default function Hierarchy(props: { hierarchy: IHierarchyElement }) {
    let [selected, setSelected] = createSignal<IHierarchyElement | undefined>();
    let [curHierarchy, setCurHierarchy] = createSignal<IHierarchyElement>(
        props.hierarchy
    );
    createEffect(() => {
        if (JSON.stringify(props.hierarchy) != JSON.stringify(curHierarchy())) {
            setCurHierarchy(props.hierarchy);
        }
    });

    return (
        <div class={styles.hierarchy}>
            <h2>Hierarchy</h2>
            <div class={styles.hierarchyList}>
                <HierarchyElement
                    element={curHierarchy()}
                    setSelected={setSelected}
                ></HierarchyElement>
            </div>
            <h2>{selected() == undefined ? "Inspector" : selected()?.name} </h2>
            <div class={styles.inspector}>
                <For each={selected()?.components}>
                    {(item) => {
                        return (
                            <div class={styles.component}>
                                <h4>{item.name}</h4>
                                <div>
                                    <For each={item.fields}>
                                        {(i) => {
                                            return (
                                                <div class={styles.field}>
                                                    <span>{i.name + ":"}</span>
                                                    <span
                                                        class={styles.fieldType}
                                                    >
                                                        {
                                                            i.type.split(".")[
                                                            i.type.split(
                                                                "."
                                                            ).length - 1
                                                                ]
                                                        }
                                                    </span>
                                                    <span>{i.value}</span>
                                                </div>
                                            );
                                        }}
                                    </For>
                                </div>
                            </div>
                        );
                    }}
                </For>
            </div>
        </div>
    );
}

function HierarchyElement(props: {
    element: IHierarchyElement;
    setSelected: Setter<IHierarchyElement | undefined>;
}) {
    let elem: HTMLDivElement | undefined;
    return (
        <div
            ref={elem}
            class={styles.hierarchyElement}
            onclick={(e) => {
                if (e.target == elem || e.target.parentElement == elem)
                    props.setSelected(props.element);
            }}
        >
            <span>{"> " + props.element.name}</span>
            <div class={styles.elementChildrenList}>
                <For each={props.element.children}>
                    {(item: IHierarchyElement) => {
                        return (
                            <HierarchyElement
                                element={item}
                                setSelected={props.setSelected}
                            ></HierarchyElement>
                        );
                    }}
                </For>
            </div>
        </div>
    );
}
