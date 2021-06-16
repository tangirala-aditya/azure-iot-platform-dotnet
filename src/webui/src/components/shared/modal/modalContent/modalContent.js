// Copyright (c) Microsoft. All rights reserved.

import React from "react";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./modalContent.module.scss"));

/** A presentational component containing the content of the modal */
export const ModalContent = ({ children, className }) => (
    <div className={css("modal-content", className)}>{children}</div>
);
