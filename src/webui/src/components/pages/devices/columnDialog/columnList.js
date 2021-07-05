import * as React from "react";
import { FocusZone, FocusZoneDirection } from "@fluentui/react/lib/FocusZone";
import { List } from "@fluentui/react/lib/List";
import {
    mergeStyleSets,
    getTheme,
    getFocusStyle,
} from "@fluentui/react/lib/Styling";
import { Label } from "@fluentui/react/lib/Label";

const theme = getTheme();
const { palette, semanticColors, fonts } = theme;
const classNames = mergeStyleSets({
    container: {
        overflow: "auto",
        maxHeight: 500,
        width: 200,
    },
    itemCell: [
        getFocusStyle(theme, { inset: -1 }),
        {
            minHeight: 25,
            padding: 2,
            boxSizing: "border-box",
            borderBottom: `1px solid ${semanticColors.bodyDivider}`,
            display: "flex",
            selectors: {
                "&:hover": { background: palette.neutralLight },
            },
        },
    ],
    itemImage: {
        flexShrink: 0,
    },
    itemContent: {
        marginLeft: 10,
        overflow: "hidden",
        flexGrow: 1,
    },
    itemName: [
        fonts.xLarge,
        {
            whiteSpace: "nowrap",
            overflow: "hidden",
            textOverflow: "ellipsis",
        },
    ],
    itemIndex: {
        fontSize: fonts.small.fontSize,
        color: palette.neutralTertiary,
        marginBottom: 0,
    },
    chevron: {
        alignSelf: "center",
        marginLeft: 5,
        color: palette.neutralTertiary,
        fontSize: fonts.small.fontSize,
        flexShrink: 0,
    },
});

const onRenderCell = (item, index) => {
    return (
        <div className={classNames.itemCell} data-is-focusable={true}>
            <div className={classNames.itemContent}>
                <div className={classNames.itemName}>{item.name}</div>
                <div className={classNames.itemIndex}>{`Item ${index}`}</div>
            </div>
        </div>
    );
};

export const ColumnList = (props) => {
    const items = [
        { id: 1, name: "Value 1" },
        { id: 2, name: "Value 2" },
        { id: 3, name: "Value 3" },
        { id: 4, name: "Value 4" },
        { id: 5, name: "Value 5" },
        { id: 6, name: "Value 6" },
        { id: 7, name: "Value 7" },
        { id: 8, name: "Value 8" },
        { id: 9, name: "Value 9" },
    ];

    return (
        <FocusZone direction={FocusZoneDirection.vertical}>
            <Label>{props.title}</Label>
            <div className={classNames.container} data-is-scrollable>
                <List items={items} onRenderCell={onRenderCell} />
            </div>
        </FocusZone>
    );
};
