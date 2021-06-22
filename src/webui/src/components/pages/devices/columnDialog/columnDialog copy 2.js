import * as React from "react";
import { Dialog, DialogType, DialogFooter } from "@fluentui/react/lib/Dialog";
import { PrimaryButton, DefaultButton } from "@fluentui/react/lib/Button";
import { Stack } from "@fluentui/react/lib/Stack";
import { ContextualMenu } from "@fluentui/react/lib/ContextualMenu";
import { IconButton } from "@fluentui/react/lib/Button";

import { ColumnList as List1 } from "./columnList";
import { ColumnList as List2  } from "./columnList";
const AddItem = () => (
    <IconButton
        iconProps={{ iconName: "ChevronRight" }}
        title="Add Item"
        ariaLabel="Add Item"
    />
);


const RemoveItem = () => (
    <IconButton
        iconProps={{ iconName: "ChevronLeft" }}
        title="Remove Item"
        ariaLabel="Remove Item"
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



const stackStyles = {
  root: {
    height: 250,
  },
};


export const ColumnDialog = (props) => {        
    const modalProps = {
            isBlocking: true,
            styles: modalPropsStyles,
            dragOptions: dragOptions,
        }

    return (
        <>
            <Dialog
                hidden={!props.show}
                onDismiss={props.toggle}
                dialogContentProps={dialogContentProps}
                modalProps={modalProps}
            >
                <Stack horizontal>
                    <Stack.Item>
                        <List1 title="Available columns" />
                    </Stack.Item>
                    <Stack.Item styles={{ stackStyles }}>
                        <Stack verticalAlign="center" styles={{ stackStyles }}>
                            <AddItem />
                            <RemoveItem />
                        </Stack>
                    </Stack.Item>
                    <Stack.Item>
                        <List2 title="Visible columns" />
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
