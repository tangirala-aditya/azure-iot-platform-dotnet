// Copyright (c) Microsoft. All rights reserved.

import React, { Component } from "react";

import { Btn, BtnToolbar, Modal } from "components/shared";
import { svgs, copyToClipboard } from "utilities";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./copyModal.module.scss"));

export class CopyModal extends Component {
    constructor(props) {
        super(props);
        this.state = {};
    }

    genericCloseClick = () => {
        const { onClose } = this.props;
        onClose();
    };

    copyAndClose = (copyLink) => {
        copyToClipboard(copyLink);
        this.genericCloseClick("CopyModal_ModalClose");
    };
    render() {
        const { t, title, copyLink } = this.props;

        return (
            <Modal
                className={css("copy-modal-container")}
                onClose={() => this.genericCloseClick()}
            >
                <div className={css("copy-header-container")}>
                    <div className={css("copy-title")}>{title}</div>
                </div>
                <div className={css("copy-info")}>{copyLink}</div>
                <div className={css("copy-summary")}>
                    <BtnToolbar>
                        <Btn
                            svg={svgs.copy}
                            primary={true}
                            onClick={() => this.copyAndClose(copyLink)}
                        >
                            {t("modal.copy")}
                        </Btn>
                        <Btn
                            svg={svgs.cancelX}
                            onClick={() =>
                                this.genericCloseClick("CopyModal_CancelClick")
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
