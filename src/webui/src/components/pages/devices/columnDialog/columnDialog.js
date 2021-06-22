import * as React from 'react';
import { useId, useBoolean } from '@fluentui/react-hooks';
import {
  getTheme,
  mergeStyleSets,
  FontWeights,
  ContextualMenu,
  Modal,
} from '@fluentui/react';
import {
    PrimaryButton, 
    DefaultButton,
    IconButton,
} from "@fluentui/react/lib/Button";
import  ColumnMapper  from './columnMapper';
import { DialogFooter } from "@fluentui/react/lib/Dialog";


export const ColumnDialog = (props) => {
    const [
        isModalOpen,
        { setTrue: showModal, setFalse: hideModal },
    ] = useBoolean(props.show);

    // Normally the drag options would be in a constant, but here the toggle can modify keepInBounds
    const dragOptions = {
        moveMenuItemText: "Move",
        closeMenuItemText: "Close",
        menu: ContextualMenu,
    };

    

    // Use useId() to ensure that the IDs are unique on the page.
    // (It's also okay to use plain strings and manually ensure uniqueness.)
    const titleId = useId("title");

    return (
        <div>
            <DefaultButton onClick={showModal} text="Open Modal" />
            <Modal
                titleAriaId={titleId}
                isOpen={isModalOpen}
                onDismiss={hideModal}
                isBlocking={true}
                containerClassName={contentStyles.container}
                dragOptions={dragOptions}
            >
                <div className={contentStyles.header}>
                    <span id={titleId}>Column Options</span>
                    <IconButton
                        styles={iconButtonStyles}
                        iconProps={cancelIcon}
                        ariaLabel="Close popup modal"
                        onClick={hideModal}
                    />
                </div>
                <div className={contentStyles.body}>
                    <ColumnMapper
                        options={props.columnOptions}
                        onChange={props.onColumnChange}
                    />
                    <DialogFooter>
                        <PrimaryButton
                            onClick={props.toggle}
                            text="OK"
                            styles={{
                                root: {
                                    color: "#fff",
                                    backgroundColor: "#f00",
                                    selectors: {
                                        ":hover": {
                                            backgroundColor: "#8d8989",
                                            color: "#030303",
                                        },
                                        ":hover .childElement": {
                                            backgroundColor: "#8d8989",
                                            color: "#030303",
                                        },
                                    },
                                },
                            }}
                        />
                        <DefaultButton onClick={props.toggle} text="Cancel" />
                    </DialogFooter>
                </div>
            </Modal>
        </div>
    );
};

const cancelIcon= { iconName: "Cancel" };

const theme = getTheme();
const contentStyles = mergeStyleSets({
    container: {
        display: "flex",
        flexFlow: "column nowrap",
        alignItems: "stretch",
        width: "700px",
    },
    header: [
        theme.fonts.xLarge,
        {
            flex: "1 1 auto",
            color: theme.palette.neutralPrimary,
            display: "flex",
            alignItems: "center",
            fontWeight: FontWeights.semibold,
            padding: "12px 12px 14px 24px",
        },
    ],
    body: {
        flex: "4 4 auto",
        padding: "0 24px 24px 24px",        
        overflowY: "hidden",
        selectors: {
            p: { margin: "14px 0" },
            "p:first-child": { marginTop: 0 },
            "p:last-child": { marginBottom: 0 },
        },
    },
});

const iconButtonStyles = {
    root: {
        color: theme.palette.neutralPrimary,
        marginLeft: "auto",
        marginTop: "4px",
        marginRight: "2px",
    },
    rootHovered: {
        color: theme.palette.neutralDark,
    },
};