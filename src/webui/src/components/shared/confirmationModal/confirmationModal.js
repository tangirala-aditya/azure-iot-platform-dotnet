// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { Btn, BtnToolbar, Modal } from "components/shared";
import { svgs } from "utilities";
import { toSinglePropertyDiagnosticsModel } from "services/models";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./confirmationModal.module.scss"));

export class ConfirmationModal extends Component {
    genericCloseClick = (eventName) => {
        const { onCancel, logEvent } = this.props;
        logEvent(
            toSinglePropertyDiagnosticsModel(
                eventName,
                "ConfirmationModal_CloseClick"
            )
        );
        onCancel();
    };

    onConfirmation = (eventName) => {
        const { onOk, logEvent } = this.props;
        logEvent(
            toSinglePropertyDiagnosticsModel(
                eventName,
                "ConfirmationModal_OkClick"
            )
        );
        onOk();
    };

    render() {
        const { t, title, confirmationInfo } = this.props;

        return (
            <Modal
                onClose={() =>
                    this.genericCloseClick("ConfirmationModal_ModalClose")
                }
                className={css("confirmation-modal-container")}
            >
                <div className={css("confirmation-header-container")}>
                    <div className={css("confirmation-title")}>{title}</div>
                </div>
                <div className={css("confirmation-info")}>
                    {confirmationInfo}
                </div>
                <div className={css("confirmation-summary")}>
                    <BtnToolbar>
                        <Btn
                            svg={svgs.apply}
                            primary={true}
                            onClick={() =>
                                this.onConfirmation("ConfirmationModal_OkClick")
                            }
                        >
                            {t("modal.ok")}
                        </Btn>
                        <Btn
                            svg={svgs.cancelX}
                            onClick={() =>
                                this.genericCloseClick(
                                    "ConfirmationModal_CancelClick"
                                )
                            }
                        >
                            {t("modal.cancel")}
                        </Btn>
                    </BtnToolbar>
                </div>
            </Modal>
        );
    }
}
