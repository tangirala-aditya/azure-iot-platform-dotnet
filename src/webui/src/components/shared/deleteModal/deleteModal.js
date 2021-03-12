// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import {
    AjaxError,
    Btn,
    BtnToolbar,
    Indicator,
    Modal,
} from "components/shared";
import { svgs } from "utilities";
import { toSinglePropertyDiagnosticsModel } from "services/models";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./deleteModal.module.scss"));

export class DeleteModal extends Component {
    constructor(props) {
        super(props);

        this.state = {
            changesApplied: false,
        };
    }

    UNSAFE_componentWillReceiveProps({ error, isPending, onDelete }) {
        if (this.state.changesApplied && !error && !isPending) {
            onDelete();
        }
    }

    apply = () => {
        const { deleteItem, itemId, logEvent } = this.props;
        if (typeof itemId !== "object") {
            this.logAndDelete(deleteItem, itemId, logEvent);
        } else {
            itemId.flatMap((itemId) => {
                this.logAndDelete(deleteItem, itemId, logEvent);
                return null;
            });
        }

        this.setState({ changesApplied: true });
    };

    logAndDelete = (deleteItem, itemId, logEvent) => {
        logEvent(
            toSinglePropertyDiagnosticsModel(
                "DeleteModal_DeleteClick",
                "ItemId",
                itemId
            )
        );
        deleteItem(itemId);
    };

    genericCloseClick = (eventName) => {
        const { onClose, itemId, logEvent } = this.props;
        logEvent(
            toSinglePropertyDiagnosticsModel(
                eventName,
                "DeleteModal_CloseClick",
                "ItemId",
                itemId
            )
        );
        onClose();
    };

    render() {
        const { t, isPending, error, title, deleteInfo } = this.props,
            { changesApplied } = this.state;

        return (
            <Modal
                onClose={() => this.genericCloseClick("DeleteModal_ModalClose")}
                className={css("delete-modal-container")}
            >
                <div className={css("delete-header-container")}>
                    <div className={css("delete-title")}>{title}</div>
                    <Btn
                        className={css("delete-close-btn")}
                        title={t("modal.cancel")}
                        onClick={() =>
                            this.genericCloseClick("DeleteModal_CloseClick")
                        }
                        svg={svgs.x}
                    />
                </div>
                <div className={css("delete-info")}>{deleteInfo}</div>
                <div className={css("delete-summary")}>
                    {!changesApplied && (
                        <BtnToolbar>
                            <Btn
                                svg={svgs.trash}
                                primary={true}
                                onClick={this.apply}
                            >
                                {t("modal.delete")}
                            </Btn>
                            <Btn
                                svg={svgs.cancelX}
                                onClick={() =>
                                    this.genericCloseClick(
                                        "DeleteModal_CancelClick"
                                    )
                                }
                            >
                                {t("modal.cancel")}
                            </Btn>
                        </BtnToolbar>
                    )}
                    {isPending && <Indicator />}
                    {changesApplied && error && (
                        <AjaxError
                            className={css("delete-error")}
                            t={t}
                            error={error}
                        />
                    )}
                </div>
            </Modal>
        );
    }
}
