(defun c:Layerchangereplateline ( / laylist lay i srcIdx dstIdx srcLayer dstLayer ss)

  ;; ===== LẤY DANH SÁCH LAYER THEO STT GỐC =====
  (setq laylist '())
  (setq lay (tblnext "LAYER" T))
  (setq i 1)

  (princ "\nDanh sách layer:")

  (while lay
    (setq laylist (append laylist (list (cdr (assoc 2 lay)))))
    (princ (strcat "\n " (itoa i) ". " (cdr (assoc 2 lay))))
    (setq i (1+ i))
    (setq lay (tblnext "LAYER"))
  )

  ;; ===== CHỌN LAYER NGUỒN =====
  (setq srcIdx (getint "\nNhập STT layer NGUỒN: "))
  (if (or (null srcIdx) (< srcIdx 1) (> srcIdx (length laylist)))
    (progn (princ "\n✖ STT layer nguồn không hợp lệ.") (exit))
  )
  (setq srcLayer (nth (1- srcIdx) laylist))

  ;; ===== CHỌN LAYER ĐÍCH =====
  (setq dstIdx (getint "\nNhập STT layer ĐÍCH: "))
  (if (or (null dstIdx) (< dstIdx 1) (> dstIdx (length laylist)))
    (progn (princ "\n✖ STT layer đích không hợp lệ.") (exit))
  )
  (setq dstLayer (nth (1- dstIdx) laylist))

  ;; ===== LẤY TẤT CẢ LINE THUỘC LAYER NGUỒN =====
  (setq ss
    (ssget "X"
      (list
        (cons 0 "LINE,LWPOLYLINE,POLYLINE,ARC,CIRCLE")

        (cons 8 srcLayer)
      )
    )
  )

  (if (null ss)
    (progn
      (princ "\n✖ Không có LINE nào trong layer nguồn.")
      (exit)
    )
  )

  ;; ===== ĐỔI LAYER =====
  (command "_.CHPROP" ss "" "_LA" dstLayer "")

  (princ
    (strcat
      "\n✔ Đã chuyển "
      (itoa (sslength ss))
      " LINE từ layer "
      srcLayer
      " sang "
      dstLayer
    )
  )
  (princ)
)
