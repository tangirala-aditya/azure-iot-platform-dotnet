import * as React from "react";
import {
    getTheme,
    mergeStyleSets,
    FontWeights,
    ContextualMenu,
    Modal,
} from "@fluentui/react";
import {
    PrimaryButton,
    DefaultButton,
    IconButton,
} from "@fluentui/react/lib/Button";
import ColumnMapper from "./columnMapper";
import { DialogFooter } from "@fluentui/react/lib/Dialog";
import { permissions } from "services/models";
import { Protected } from "components/shared";

// Normally the drag options would be in a constant, but here the toggle can modify keepInBounds
const dragOptions = {
    moveMenuItemText: "Move",
    closeMenuItemText: "Close",
    menu: ContextualMenu,
};

export class ColumnDialog extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            selectedOptions: props.selectedOptions,
        };
    }

    onSelectionChange = (selected) => {
        this.setState({ selectedOptions: selected });
    };

    applyChanges = (saveUpdates = false) => {
        this.props.updateColumns(saveUpdates, this.state.selectedOptions);
    };

    render() {
        const { toggle, columnOptions, show, t } = this.props;
        return (
            <div>
                <Modal
                    titleAriaId="columnOptionsModal"
                    isOpen={show}
                    isBlocking={true}
                    containerClassName={contentStyles.container}
                    dragOptions={dragOptions}
                >
                    <div className={contentStyles.header}>
                        <span>{t("devices.columnOptions")}</span>
                        <IconButton
                            onClick={toggle}
                            styles={iconButtonStyles}
                            iconProps={cancelIcon}
                            ariaLabel="Close popup modal"
                        />
                    </div>
                    <div className={contentStyles.body}>
                        <ColumnMapper
                            options={columnOptions}
                            selected={this.state.selectedOptions}
                            onChange={this.onSelectionChange}
                            preserveSelectOrder
                            showOrderButtons
                            canFilter
                        />
                        <DialogFooter>
                            <Protected
                                permission={permissions.createDeviceGroups}
                            >
                                <PrimaryButton
                                    onClick={() => this.applyChanges(true)}
                                    title="Apply and save"
                                    text="Save"
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
                            </Protected>
                            <PrimaryButton
                                onClick={() => this.applyChanges(false)}
                                title="Apply"
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
                            <DefaultButton onClick={toggle} text="Cancel" />
                        </DialogFooter>
                    </div>
                </Modal>
            </div>
        );
    }
}

const cancelIcon = { iconName: "Cancel" };

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
