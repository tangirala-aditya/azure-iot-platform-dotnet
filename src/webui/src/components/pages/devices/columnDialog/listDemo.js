import * as React from "react";
import { FocusZone, FocusZoneDirection } from "@fluentui/react/lib/FocusZone";
import { DefaultButton } from "@fluentui/react/lib/Button";
import { Dropdown, IDropdownOption } from "@fluentui/react/lib/Dropdown";
import { List, ScrollToMode, IList } from "@fluentui/react/lib/List";
import { TextField } from "@fluentui/react/lib/TextField";
import { createListItems, IExampleItem } from "@fluentui/example-data";
import {
    mergeStyleSets,
    getTheme,
    normalize,
} from "@fluentui/react/lib/Styling";
import { useConst } from "@fluentui/react-hooks";

const evenItemHeight = 25;
const oddItemHeight = 50;
const numberOfItemsOnPage = 10;
const theme = getTheme();
const dropdownOptions = [
    { key: "auto", text: "Auto" },
    { key: "top", text: "Top" },
    { key: "bottom", text: "Bottom" },
    { key: "center", text: "Center" },
];

const styles = mergeStyleSets({
    container: {
        overflow: "auto",
        maxHeight: 400,
        border: "1px solid #CCC",
        marginTop: 20,
        selectors: {
            ".ms-List-cell:nth-child(odd)": {
                height: 50,
                lineHeight: 50,
                background: theme.palette.neutralLighter,
            },
            ".ms-List-cell:nth-child(even)": {
                height: 25,
                lineHeight: 25,
            },
        },
    },
    itemContent: [
        theme.fonts.medium,
        normalize,
        {
            position: "relative",
            boxSizing: "border-box",
            display: "block",
            borderLeft: "3px solid " + theme.palette.themePrimary,
            paddingLeft: 27,
        },
    ],
});

const onRenderCell = (item, index) => {
    return (
        <div data-is-focusable>
            <div className={styles.itemContent}>
                {index} &nbsp; {item.name}
            </div>
        </div>
    );
};

export const ListScrollingExample = () => {
    const items = useConst(() => createListItems(5000));
    const [selectedIndex, setSelectedIndex] = React.useState(0);
    const [scrollToMode, setScrollToMode] =
        React.useState < ScrollToMode > ScrollToMode.auto;
    const listRef = React.useRef(null);

    const scroll = (index, propScrollToMode) => {
        const updatedSelectedIndex = Math.min(
            Math.max(index, 0),
            items.length - 1
        );
        setSelectedIndex(updatedSelectedIndex);
        setScrollToMode(propScrollToMode);

        listRef.current?.scrollToIndex(
            updatedSelectedIndex,
            (idx) => (idx % 2 === 0 ? evenItemHeight : oddItemHeight),
            scrollToMode
        );
    };

    const getPageHeight = (idx)=> {
        let h = 0;
        for (let i = idx; i < idx + numberOfItemsOnPage; ++i) {
            const isEvenRow = i % 2 === 0;

            h += isEvenRow ? evenItemHeight : oddItemHeight;
        }
        return h;
    };

    const scrollRelative = (delta) => {
        return () => {
            scroll(selectedIndex + delta, scrollToMode);
        };
    };

    const onChangeText = (
        ev,
        value
    ) => {
        scroll(parseInt(value, 10) || 0, scrollToMode);
    };

    const onDropdownChange = (
        event,
        option
    ) => {
        let scrollMode = scrollToMode;
        switch (option.key) {
            case "auto":
                scrollMode = ScrollToMode.auto;
                break;
            case "top":
                scrollMode = ScrollToMode.top;
                break;
            case "bottom":
                scrollMode = ScrollToMode.bottom;
                break;
            case "center":
                scrollMode = ScrollToMode.center;
                break;
        }
        scroll(selectedIndex, scrollMode);
    };

    return (
        <FocusZone direction={FocusZoneDirection.vertical}>
            <div>
                <DefaultButton onClick={scrollRelative(-10)}>-10</DefaultButton>
                <DefaultButton onClick={scrollRelative(-1)}>-1</DefaultButton>
                <DefaultButton onClick={scrollRelative(1)}>+1</DefaultButton>
                <DefaultButton onClick={scrollRelative(10)}>+10</DefaultButton>
            </div>
            <Dropdown
                placeholder="Select an Option"
                label="Scroll To Mode:"
                ariaLabel="Scroll To Mode"
                defaultSelectedKey={"auto"}
                options={dropdownOptions}
                // eslint-disable-next-line react/jsx-no-bind
                onChange={onDropdownChange}
            />
            <div>
                Scroll item index:
                <TextField
                    value={selectedIndex.toString(10)}
                    // eslint-disable-next-line react/jsx-no-bind
                    onChange={onChangeText}
                />
            </div>
            <div className={styles.container} data-is-scrollable>
                <List
                    componentRef={listRef}
                    items={items}
                    // eslint-disable-next-line react/jsx-no-bind
                    getPageHeight={getPageHeight}
                    onRenderCell={onRenderCell}
                />
            </div>
        </FocusZone>
    );
};
