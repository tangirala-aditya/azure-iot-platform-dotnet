// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./modalFadeBox.module.scss"));

/** A presentational component containing the content of the modal */
export const ModalFadeBox = ({ children, className, onClick }) => (
    <div onClick={onClick} className={css("modal-fade-box", className)}>
        {children}
    </div>
);
