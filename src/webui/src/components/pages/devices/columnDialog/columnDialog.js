import * as React from "react";
import { Dialog, DialogType, DialogFooter } from "@fluentui/react/lib/Dialog";
import { PrimaryButton, DefaultButton } from "@fluentui/react/lib/Button";
import { Stack } from "@fluentui/react/lib/Stack";
import { ContextualMenu } from "@fluentui/react/lib/ContextualMenu";
import { Label } from "@fluentui/react/lib/Label";
import { IconButton } from "@fluentui/react/lib/Button";
import {
    ResizeGroupDirection,
    ResizeGroup,
    OverflowSet,
    DirectionalHint,
    createArray,
} from "@fluentui/react";
import { CommandBarButton } from "@fluentui/react/lib/Button";

const AddItem = () => (
    <IconButton
        iconProps={{ iconName: "ChevronRight" }}
        title="Add Item"
        ariaLabel="Add Item"
    />
);
const buttonStyles = {
    root: {
        paddingBottom: 10,
        paddingTop: 10,
        width: 100,
    },
};

//const exampleHeight = "40vh";
//const resizeRootClassName = mergeStyles({ height: exampleHeight });

const onRenderOverflowButton = (overflowItems) => (
  <CommandBarButton
    role="menuitem"
    styles={buttonStyles}
    menuIconProps={{ iconName: 'ChevronRight' }}
    menuProps={{ items: overflowItems, directionalHint: DirectionalHint.rightCenter }}
  />
);
const RemoveItem = () => (
    <IconButton
        iconProps={{ iconName: "ChevronLeft" }}
        title="Remove Item"
        ariaLabel="Remove Item"
    />
);

const onRenderItem = (item) => (
    <CommandBarButton
        role="menuitem"
        text={item.name}
        iconProps={{ iconName: item.icon }}
        onClick={item.onClick}
        checked={item.checked}
        styles={buttonStyles}
    />
);
//ChevronLeft
//<i class="ms-Icon ms-Icon--ChevronLeft" aria-hidden="true"></i>
//import { useBoolean } from "@fluentui/react-hooks";

const dragOptions = {
    moveMenuItemText: "Move",
    closeMenuItemText: "Close",
    menu: ContextualMenu,
};
const modalPropsStyles = { main: { maxWidth: 1200, width: "800px" } };
const dialogContentProps = {
    type: DialogType.normal,
    title: "Column options",
};


const onRenderData = (data) => (
  <OverflowSet
    role="menubar"
    vertical
    items={data.primary}
    overflowItems={data.overflow.length ? data.overflow : null}
    onRenderItem={onRenderItem}
    onRenderOverflowButton={onRenderOverflowButton}
  />
);

const onReduceData = (currentData) => {
  if (currentData.primary.length === 0) {
    return undefined;
  }
  const overflow = [...currentData.primary.slice(-1), ...currentData.overflow];
  const primary = currentData.primary.slice(0, -1);
  return { primary, overflow };
};


const generateData = (count, cachingEnabled, checked) => {
    const icons = ["Add", "Share", "Upload"];
    let cacheKey = "";
    const dataItems = createArray(count, (index) => {
        cacheKey = cacheKey + `item${index}`;
        return {
            key: `item${index}`,
            name: `Item ${index}`,
            icon: icons[index % icons.length],
            checked: checked,
        };
    });
    let result = {
        primary: dataItems,
        overflow: [],
    };
    if (cachingEnabled) {
        result = { ...result, cacheKey };
    }
    return result;
};

const dataToRender = generateData(8, false, false);

const stackStyles = {
  root: {
    height: 250,
  },
};

export const ColumnDialog = (props) => {
    //const [hideDialog, { toggle: toggleHideDialog }] = useBoolean(true);
    const isDraggable = true;    
    const modalProps = React.useMemo(
        () => ({
            isBlocking: true,
            styles: modalPropsStyles,
            dragOptions: dragOptions,
        }),
        [isDraggable]
    );

    return (
        <>
            <Dialog
                hidden={!props.show}
                onDismiss={props.toggle}
                dialogContentProps={dialogContentProps}
                modalProps={modalProps}
            >
                <Stack horizontal >
                    <Stack.Item>
                        <Label>Available columns</Label>
                        <ResizeGroup
                            role="tabpanel"
                            aria-label="Vertical Resize Group with an Overflow Set"
                            direction={ResizeGroupDirection.vertical}
                            data={dataToRender}
                            onReduceData={onReduceData}
                            onRenderData={onRenderData}
                        />
                    </Stack.Item>
                    <Stack.Item styles={{ stackStyles }}>
                        <Stack verticalAlign="center" styles={{ stackStyles }}>
                            <AddItem />
                            <RemoveItem />
                        </Stack>
                    </Stack.Item>
                    <Stack.Item>
                        <Label>Selected columns</Label>
                    </Stack.Item>
                </Stack>
                <DialogFooter>
                    <PrimaryButton onClick={props.toggle} text="OK" />
                    <DefaultButton onClick={props.toggle} text="Cancel" />
                </DialogFooter>
            </Dialog>
        </>
    );
};
