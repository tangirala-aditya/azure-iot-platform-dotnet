// Copyright (c) Microsoft. All rights reserved.

import React from "react";

import { joinClasses } from "utilities";

// import styles from "./modalFadeBox.module.scss";

const classnames = require("classnames/bind");
const css = classnames.bind(require("./modalFadeBox.module.scss"));

/** A presentational component containing the content of the modal */
export const ModalFadeBox = ({ children, className, onClick }) => (
    <div
        onClick={onClick}
        className={joinClasses(css("modal-fade-box"), className)}
    >
        {children}
    </div>
);
