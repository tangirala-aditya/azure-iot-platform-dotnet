// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

// import styles from "./modalContent.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./modalContent.module.scss"));

/** A presentational component containing the content of the modal */
export const ModalContent = ({ children, className }) => (
    <div className={joinClasses(css("modal-content"), className)}>
        {children}
    </div>
);
